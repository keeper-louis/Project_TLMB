using KEEPER.K3.TLMB.Contracts;
using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.App
{
    public class CommonService:ICommonService
    {
        public DynamicObject GetBasicObject(Context ctx, string formID, long ObjectID)
        {
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
            FormMetadata Meta = metaService.Load(ctx, formID) as FormMetadata;//获取基础资料元数据
            DynamicObject BasicObject = view.LoadSingle(ctx, ObjectID, Meta.BusinessInfo.GetDynamicObjectType());
            return BasicObject;
        }

        public IOperationResult SubmitBill(Context ctx, string formID, object[] ids)
        {
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            FormMetadata Meta = metaService.Load(ctx, formID) as FormMetadata;//获取元数据
            OperateOption submitOption = OperateOption.Create();
            IOperationResult submitResult = BusinessDataServiceHelper.Submit(ctx, Meta.BusinessInfo, ids, "Submit", submitOption);
            return submitResult;
        }

        public IOperationResult AuditBill(Context ctx, string FormID, object[] ids)
        {
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            FormMetadata Meta = metaService.Load(ctx, FormID) as FormMetadata;//获取元数据
            OperateOption AuditOption = OperateOption.Create();
            IOperationResult AuditResult = BusinessDataServiceHelper.Audit(ctx, Meta.BusinessInfo, ids, AuditOption);
            return AuditResult;
        }
        public void Log(Context ctx, string Operation, IOperationResult returnResult)
        {
            string strSql = string.Empty;
            try
            {

                strSql = string.Format(@"/*dialect*/INSERT INTO KEEPER_LOG VALUES('{0}','{1}','{2}','{3}')", DateTime.Now, returnResult.OperateResult[0].Number, Operation, returnResult.OperateResult[0].Message);
                DBUtils.Execute(ctx, strSql);

            }
            catch (Exception e)
            {
                Logger.Error(Operation, "日志记录失败" + strSql, e);
            }



        }

        public IOperationResult SaveBill(Context ctx, string FormID, DynamicObject dyObject)
        {
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            FormMetadata Meta = metaService.Load(ctx, FormID) as FormMetadata;//获取元数据
            OperateOption SaveOption = OperateOption.Create();
            IOperationResult SaveResult = BusinessDataServiceHelper.Save(ctx, Meta.BusinessInfo, dyObject, SaveOption, "Save");
            return SaveResult;
        }

        public DynamicObject installCostRequestPackage(Context ctx, string FormID, Action<IDynamicFormViewService> fillBillPropertys, string BillTypeId = "")
        {
            //IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            //FormMetadata Meta = metaService.Load(ctx, FormID) as FormMetadata;//获取元数据
            FormMetadata Meta = MetaDataServiceHelper.Load(ctx, FormID) as FormMetadata;//获取元数据
            Form form = Meta.BusinessInfo.GetForm();
            IDynamicFormViewService dynamicFormViewService = (IDynamicFormViewService)Activator.CreateInstance(Type.GetType("Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web"));
            // 创建视图加载参数对象，指定各种参数，如FormId, 视图(LayoutId)等
            BillOpenParameter openParam = new BillOpenParameter(form.Id, Meta.GetLayoutInfo().Id);
            openParam.Context = ctx;
            openParam.ServiceName = form.FormServiceName;
            openParam.PageId = Guid.NewGuid().ToString();
            openParam.FormMetaData = Meta;
            openParam.Status = OperationStatus.ADDNEW;
            openParam.CreateFrom = CreateFrom.Default;
            // 单据类型
            openParam.DefaultBillTypeId = BillTypeId;
            openParam.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            // 插件
            List<AbstractDynamicFormPlugIn> plugs = form.CreateFormPlugIns();
            openParam.SetCustomParameter(FormConst.PlugIns, plugs);
            PreOpenFormEventArgs args = new PreOpenFormEventArgs(ctx, openParam);
            foreach (var plug in plugs)
            {
                plug.PreOpenForm(args);
            }
            // 动态领域模型服务提供类，通过此类，构建MVC实例
            IResourceServiceProvider provider = form.GetFormServiceProvider(false);

            dynamicFormViewService.Initialize(openParam, provider);
            IBillView billView = dynamicFormViewService as IBillView;
            ((IBillViewService)billView).LoadData();

            // 触发插件的OnLoad事件：
            // 组织控制基类插件，在OnLoad事件中，对主业务组织改变是否提示选项进行初始化。
            // 如果不触发OnLoad事件，会导致主业务组织赋值不成功
            DynamicFormViewPlugInProxy eventProxy = billView.GetService<DynamicFormViewPlugInProxy>();
            eventProxy.FireOnLoad();
            if (fillBillPropertys != null)
            {
                fillBillPropertys(dynamicFormViewService);
            }
            // 设置FormId
            form = billView.BillBusinessInfo.GetForm();
            if (form.FormIdDynamicProperty != null)
            {
                form.FormIdDynamicProperty.SetValue(billView.Model.DataObject, form.Id);
            }
            return billView.Model.DataObject;
        }


        /// <summary>
        /// 获取销售政策内容
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="deptId">销售部门</param>
        /// <param name="Date">业务日期</param>
        /// <returns></returns>
        public Dictionary<string, double> GetDiscounts(Context ctx, long deptId, DateTime Date)
        {
            Dictionary<string, double> disCounts = new Dictionary<string, double>();
            //0面包，1粽子，2月饼
            //获取部门面包折扣
            string breadDiscountSql = string.Format(@"select distinct bb.fcategory, bb.fdiscount
  from paez_salepolicy aa
 inner join paez_salepolicyentry bb
    on aa.fid = bb.fid
   and aa.fcreatedate =
       (select max(a.fcreatedate)
          from paez_salepolicy a
         inner join paez_salepolicyentry b
            on a.fid = b.fid
         where to_date('{0}','yyyy/MM/dd') >= a.fstartdate
           and to_date('{0}','yyyy/MM/dd') <= a.fenddate
           and b.fdeptid = {1}
           and b.fcategory = 0)
   and bb.fdeptid = {1} and bb.fcategory = 0", Date.ToShortDateString(),deptId);
            DynamicObjectCollection breadDiscounts = DBUtils.ExecuteDynamicObject(ctx, breadDiscountSql);
            if (breadDiscounts!=null&&breadDiscounts.Count()>0)
            {
                disCounts.Add(Convert.ToString(breadDiscounts[0]["fcategory"]), Convert.ToDouble(breadDiscounts[0]["fdiscount"]));
            }
            //获取部门月饼折扣
            string moonCakeDiscountSql = string.Format(@"select distinct bb.fcategory, bb.fdiscount
  from paez_salepolicy aa
 inner join paez_salepolicyentry bb
    on aa.fid = bb.fid
   and aa.fcreatedate =
       (select max(a.fcreatedate)
          from paez_salepolicy a
         inner join paez_salepolicyentry b
            on a.fid = b.fid
         where to_date('{0}','yyyy/MM/dd') >= a.fstartdate
           and to_date('{0}','yyyy/MM/dd') <= a.fenddate
           and b.fdeptid = {1}
           and b.fcategory = 2)
   and bb.fdeptid = {1} and bb.fcategory = 2", Date.ToShortDateString(), deptId);
            DynamicObjectCollection moonCakeDiscounts = DBUtils.ExecuteDynamicObject(ctx, moonCakeDiscountSql);
            if (moonCakeDiscounts != null && moonCakeDiscounts.Count() > 0)
            {
                disCounts.Add(Convert.ToString(moonCakeDiscounts[0]["fcategory"]), Convert.ToDouble(moonCakeDiscounts[0]["fdiscount"]));
            }
            //获取部门粽子折扣
            string ChineseCakeDiscountSql = string.Format(@"select distinct bb.fcategory, bb.fdiscount
  from paez_salepolicy aa
 inner join paez_salepolicyentry bb
    on aa.fid = bb.fid
   and aa.fcreatedate =
       (select max(a.fcreatedate)
          from paez_salepolicy a
         inner join paez_salepolicyentry b
            on a.fid = b.fid
         where to_date('{0}','yyyy/MM/dd') >= a.fstartdate
           and to_date('{0}','yyyy/MM/dd') <= a.fenddate
           and b.fdeptid = {1}
           and b.fcategory = 1)
   and bb.fdeptid = {1} and bb.fcategory = 1", Date.ToShortDateString(), deptId);
            DynamicObjectCollection ChineseCakeDiscounts = DBUtils.ExecuteDynamicObject(ctx, ChineseCakeDiscountSql);
            if (ChineseCakeDiscounts != null && ChineseCakeDiscounts.Count() > 0)
            {
                disCounts.Add(Convert.ToString(ChineseCakeDiscounts[0]["fcategory"]), Convert.ToDouble(ChineseCakeDiscounts[0]["fdiscount"]));
            }
            return disCounts;
        }

        /// <summary>
        /// 禁用单据
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="FormID">单据标识</param>
        /// <param name="ids">禁用单据内码集合</param>
        /// <param name="pkId">表主键列</param>
        /// <param name="tableName">表名</param>
        /// <param name="fieldName">禁用状态列</param>
        /// <param name="fieldValue">禁用值</param>
        /// <returns></returns>
        public void SetState(Context ctx, string FormID, object[] ids, string pkId, string tableName, string fieldName, string fieldValue)
        {
            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            FormMetadata Meta = metaService.Load(ctx, FormID) as FormMetadata;//获取元数据
            OperateOption AuditOption = OperateOption.Create();
            BusinessDataServiceHelper.SetState(ctx, tableName, fieldName, fieldValue, pkId, ids);
        }

       /// <summary>
       /// 获取赠品价格集合
       /// </summary>
       /// <param name="ctx"></param>
       /// <param name="fdeliverydate"></param>
       /// <param name="custId"></param>
       /// <param name="saleOrgId"></param>
       /// <returns></returns>
        public Dictionary<int, double> GetPriceDictionary(Context ctx, string fdeliverydate, long custId, long saleOrgId)
        {
            Dictionary<int, double> GiftPrice = new Dictionary<int, double>();
            string strSq1 = string.Format(@"/*dialect*/with xx as
 (select zz.*,
         row_number() over(partition by zz.fmaterialid order by zz.fdefaultpriceo) as group_idx
    from (select c.fmaterialid, c.fdefaultpriceo
            from t_sal_pricelist          a,
                 t_bas_assistantdataentry b,
                 t_sal_pricelistentry     c,
                 t_sal_applycustomer      d,
                 t_bd_unit                e
           where a.fpricetype = b.fentryid
             and a.fid = c.fid
             and a.fid = d.fid
             and c.fpriceunitid = e.funitid
             and a.fdocumentstatus = 'C'
             and a.fforbidstatus = 'A'
             and c.fforbidstatus = 'A'
             and c.frowauditstatus = 'A'
             and a.feffectivedate <= to_date('{0}','yyyy-MM-dd')
             and a.fexpirydate >= to_date('{0}','yyyy-MM-dd')
             and c.feffectivedate <= to_date('{0}','yyyy-MM-dd')
             and c.fexpriydate >= to_date('{0}','yyyy-MM-dd')
             and a.fsaleorgid = {1}
             and ((a.flimitcustomer = '2' and
                 d.fcusttypeid =
                 (select f.fcusttypeid
                      from t_bd_customer f
                     where fcustid = {2})))
             and a.fpricetype = '57eb5de168e269') zz)
select * from xx where xx.group_idx = 1
", fdeliverydate, saleOrgId, custId);
            DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(ctx, strSq1);
            if (result!=null&&result.Count()>0)
            {
                foreach (DynamicObject item in result)
                {
                    GiftPrice.Add(Convert.ToInt32(item["fmaterialid"]), Convert.ToDouble(item["fdefaultpriceo"]));
                }
                return GiftPrice;
            }
            else
            {
                return null;
            }
            
        }
    }
}
