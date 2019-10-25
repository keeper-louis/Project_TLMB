using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.Metadata;

namespace ZQC.K3.TLMB.SalOrderToDirectConvertPlugin
{
    [Description("销售订单-直接调拨单，分析箱套种类计算箱套数量")]
    //工厂配货单据要根据配货的商品，分析承载商品的箱套类型，并根据配送商品的数量计算需要的箱套的数量。
    //开发单据转换插件，在销售订单-直接调拨单（分录合并）单据转换执行时，分析参与转换的物料需要的箱套种类，并且计算需要的箱套数量。自动新增直接调拨单分录行，将结果填写到工厂配货的直接调拨单中
    public class SalOrderToDirectConvertPlugin : AbstractConvertPlugIn
    {
        /// <summary>
        /// 主单据体的字段携带完毕，与源单的关联关系创建好之后，触发此事件
        /// </summary>
        /// <param name="e"></param>
        public override void OnAfterCreateLink(CreateLinkEventArgs e)
        {
            base.OnAfterCreateLink(e);
            //源单第一单据体
            Entity srcFirstEntity = e.SourceBusinessInfo.GetEntity("FSaleOrderPlan");

            //目标单第一单据体
            Entity mainEntity = e.TargetBusinessInfo.GetEntity("FBillEntry");
            
            // 目标单关联子单据体
            Entity linkEntity = null;
            Form form = e.TargetBusinessInfo.GetForm();
            if (form.LinkSet != null
                && form.LinkSet.LinkEntitys != null
                && form.LinkSet.LinkEntitys.Count != 0)
            {
                linkEntity = e.TargetBusinessInfo.GetEntity(
                    form.LinkSet.LinkEntitys[0].Key);
            }

            if (linkEntity == null)
            {
                return;
            }
            // 获取生成的全部下游单据
            ExtendedDataEntity[] billDataEntitys = e.TargetExtendedDataEntities.FindByEntityKey("FBillHead");

            // 对下游单据，逐张单据进行处理
            foreach (var item in billDataEntitys)
            {
                DynamicObject dataObject = item.DataEntity;

                // 定义一个集合，用于收集本单对应的源单内码
                HashSet<long> srcBillIds = new HashSet<long>();

                // 开始到主单据体中，读取关联的源单内码
                DynamicObjectCollection mainEntryRows =
                    mainEntity.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                DynamicObject mainEntityRow = mainEntryRows[0];
                DynamicObjectCollection linkRows =
                        linkEntity.DynamicProperty.GetValue(mainEntityRow) as DynamicObjectCollection;
                long srcBillId = Convert.ToInt64(linkRows[0]["SBillId"]);
                if (srcBillId != 0
                            && srcBillIds.Contains(srcBillId) == false)
                {
                    srcBillIds.Add(srcBillId);
                }
                if (srcBillIds.Count == 0)
                {
                    continue;
                }
                #region 隐藏
                //foreach (var mainEntityRow in mainEntryRows)
                //{
                //    DynamicObjectCollection linkRows =
                //        linkEntity.DynamicProperty.GetValue(mainEntityRow) as DynamicObjectCollection;
                //    foreach (var linkRow in linkRows)
                //    {
                //        long srcBillId = Convert.ToInt64(linkRow["SBillId"]);
                //        if (srcBillId != 0
                //            && srcBillIds.Contains(srcBillId) == false)
                //        {
                //            srcBillIds.Add(srcBillId);
                //        }
                //    }
                //}
                //if (srcBillIds.Count == 0)
                //{
                //    continue;
                //}
                #endregion

                // 开始加载源单第二单据体上的字段

                // 确定需要加载的源单字段（仅加载需要携带的字段）
                List<SelectorItemInfo> selector = new List<SelectorItemInfo>();
                selector.Add(new SelectorItemInfo("FDate"));//日期
               
                // TODO: 继续添加其他需要携带的字段，示例代码略
                // 设置过滤条件
                string filter = string.Format(" {0} IN ({1}) ",
                    e.SourceBusinessInfo.GetForm().PkFieldName,
                    string.Join(",", srcBillIds));
                OQLFilter filterObj = OQLFilter.CreateHeadEntityFilter(filter);

                // 读取源单
                Kingdee.BOS.Contracts.IViewService viewService = Kingdee.BOS.App.ServiceHelper.GetService<Kingdee.BOS.Contracts.IViewService>();
                var srcBillObjs = viewService.Load(this.Context,
                    e.SourceBusinessInfo.GetForm().Id,
                    selector,
                    filterObj);
                #region 隐藏
                // 开始把源单单据体数据，填写到目标单上
                //DynamicObjectCollection secondEntryRows =
                //    secondEntity.DynamicProperty.GetValue(dataObject) as DynamicObjectCollection;
                //secondEntryRows.Clear();    // 删除空行
                #endregion

                foreach (DynamicObject srcBillObj in srcBillObjs)
                {
                    DynamicObject dept = srcBillObj["SaleDeptID"] as DynamicObject;//销售部门
                    DynamicObject Cust = srcBillObj["CustomerID"] as DynamicObject;//客户
                    //bool isRight = !Cust["FKHBD"].Equals("1") && !Cust["FKHBD"].Equals("2") ? Convert.ToInt32(Cust["FSAP"]) == 2 : Convert.ToInt32(Cust["FKHBD"]) == 1 ? true: Convert.ToInt32(Cust["FKHBD"]) == 2 ? true:false;

                    bool isRight = (Cust["FKHBD"].Equals("1") || Cust["FKHBD"].Equals("2")) ? true : Convert.ToInt32(Cust["FSAP"]) == 2 ? true : false;
                    //if ((Convert.ToInt32(Cust["FSAP"]) == 2 || Convert.ToInt32(Cust["FKHBD"]) == 1 || Convert.ToInt32(Cust["FKHBD"]) == 2) && Convert.ToInt32(dept["FDEPTTYPE"]) == 4)
                    if (isRight && Convert.ToInt32(dept["FDEPTTYPE"]) == 4)
                    {
                        //外埠现金客户执行销售政策折扣
                        Dictionary<string, double> disCounts = TLMBServiceHelper.GetDiscounts(base.Context, Convert.ToInt64(dept["id"]), Convert.ToDateTime(srcBillObj["Date"]));
                        //遍历应收单，通过销售折扣计算折后的含税单价赋值。
                        foreach (DynamicObject mainEntryRow in mainEntryRows)
                        {
                            double discount = 0.0;
                            if (disCounts.TryGetValue(Convert.ToString(((DynamicObject)mainEntryRow["MATERIALID"])["FWLFL"]), out discount))
                            {
                                double ZK = Convert.ToDouble(disCounts["" + Convert.ToString(((DynamicObject)mainEntryRow["MATERIALID"])["FWLFL"]) + ""]);
                                mainEntryRow["TaxPrice"] = Convert.ToDouble(mainEntryRow["TaxPrice"]) * ZK;
                                mainEntryRow["F_PAEZ_DisCount"] = ZK;
                            }
                        }
                    }

                    //DynamicObjectCollection srcEntryRows =
                    //    srcSecondEntity.DynamicProperty.GetValue(srcBillObj) as DynamicObjectCollection;

                    //foreach (var srcEntryRow in srcEntryRows)
                    //{
                    //    // 目标单添加新行，并接受源单字段值
                    //    DynamicObject newRow = new DynamicObject(secondEntity.DynamicObjectType);
                    //    secondEntryRows.Add(newRow);
                    //    // 填写字段值
                    //    newRow["F_JD_Text"] = srcEntryRow["F_JD_Text"];
                    //    // TODO: 逐个填写其他字段值，示例代码略
                    //}
                }
            }
        }
    }
}
