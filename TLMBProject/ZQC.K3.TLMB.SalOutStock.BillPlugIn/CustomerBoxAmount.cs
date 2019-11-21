using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;

namespace ZQC.K3.TLMB.SalOutStock.BussinessPlugIn
{
    [Description("销售出库单子客户计算箱套数量")]
    public class CustomerBoxAmount : AbstractBillPlugIn
    {
        //根据组织确定其是否是分公司销售，保存前根据物料判断 并且新增行计算箱套数量添加到分录中
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);

            //1.获取单据头销售组织
            DynamicObject saleOrg = this.Model.GetValue("FSALEORGID") as DynamicObject;
            //2.获取单据头客户
            DynamicObject customer = this.Model.GetValue("FCUSTOMERID") as DynamicObject;
            
            //4.定义泡沫箱，塑料箱总数
            decimal slx=0;
            decimal pmx=0;
            //客户和销售组织不为空
            if (saleOrg!=null && customer!=null )
            {
                long useOrgId = Convert.ToInt64(saleOrg["Id"]);
                long customerId = Convert.ToInt64(customer["Id"]);
                //3.通过sql查询该组织下对应所有子客户
                string sql = string.Format(@"select t1.FcustId 
                                           from t_bd_customer t1
                                           where t1.FPRIMARYGROUP=105322  and FUSEORGID = {0}", useOrgId);
                DynamicObjectCollection allCusto = DBUtils.ExecuteDynamicObject(base.Context, sql);
                foreach (DynamicObject custo in allCusto)
                {
                   //4.如果是子客户，则需要获取分录计算
                    long custoId = Convert.ToInt64(custo["FCUSTID"]);
                    if ( custoId == customerId)
                    {
                        DynamicObject billObj = this.Model.DataObject;
                        DynamicObjectCollection entrys = billObj["SAL_OUTSTOCKENTRY"] as DynamicObjectCollection;
                        for (int i = 0; i < entrys.Count; i++)
                        {
                            //获取每一个分录
                            DynamicObject entry = entrys[i] as DynamicObject;
                            //塑料箱数量
                            decimal slxAmount =Convert.ToDecimal( entry["FSLXQTY"]);
                            if (slxAmount != 0)
                            {
                                slx = slx + slxAmount;
                            }
                            //泡沫箱数量
                            decimal pmxAmount = Convert.ToDecimal(entry["FPMXQTY"]);
                            if(pmxAmount != 0)
                            {
                                pmx = pmx + pmxAmount;
                                
                            }
                        }
                        //this.View.Model.CreateNewEntryRow("FEntity");//新增
                        //根据组织查询对应 箱套
                        string sql2 = string.Format(@"select fmaterialid from t_bd_material where fnumber='07040000' and fuseorgid={0}", useOrgId);
                        long result = DBUtils.ExecuteScalar<long>(base.Context, sql2, -1, null);
                        if (result != 0 && result != -1)
                        {
                            IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                            IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                            FormMetadata Meta = metaService.Load(base.Context, "BD_MATERIAL") as FormMetadata;//获取基础资料元数据
                            DynamicObject BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());
                            this.Model.BatchCreateNewEntryRow("FEntity",1);
                            this.Model.SetValue("MaterialId", BasicObject,entrys.Count);
                        }

                           

                    }
                    

            }
             //新增分录行
    
            }

        }
    }
}
