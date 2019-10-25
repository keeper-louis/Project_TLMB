using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.App.Data;

namespace ZQC.K3.TLMB.STOCK.ServicePlugin
{
    [Description("其他出入库库存状态改变")]
    public class SaveStateChange : AbstractOperationServicePlugIn
    {
        
        //库存状态对象
        DynamicObject BasicObject;
        //物料编码必须加载
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FMATERIALID");
        }
        //操作初始化时确认操作执行参数
        
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            string strSql = string.Format(@"/*dialect*/select A.FSTOCKSTATUSID from t_BD_StockStatus A inner join t_Bd_Stockstatus_l B on A.FSTOCKSTATUSID = B.FSTOCKSTATUSID where B.FNAME = '不参与核算'");
            long result = DBUtils.ExecuteScalar<long>(base.Context, strSql, -1, null);
            DynamicObject[] entitys = e.DataEntitys;
            if (result > 0)
            {
                IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                FormMetadata Meta = metaService.Load(base.Context, "BD_StockStatus") as FormMetadata;//获取基础资料元数据
                BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());//不参与核算的库存状态ID
            }
            foreach (DynamicObject entity in entitys)
            {//FSTOCKSTATUSID库存状态  编码  07040000 07040004
                if (entity != null)
                {

                    DynamicObjectCollection entityInfo = entity["STK_MISCELLANEOUSENTRY"] as DynamicObjectCollection;
                    for (int i = 0; i < entityInfo.Count; i++)
                    {
                        DynamicObject material = entityInfo[i]["MATERIALID"] as DynamicObject;

                        if (material["Number"].Equals("07040000") || material["Number"].Equals("07040004"))
                        {
                            if (BasicObject != null)
                            {
                                //string updateStr = string.Format(@"/*dialect*/Update T_STK_MISCELLANEOUSENTRY set FSTOCKSTATUSID = {0} where fentryid = {1}", result, Convert.ToInt64(entityInfo[i]["Id"]));
                                //DBUtils.Execute(base.Context, updateStr);
                                entityInfo[i]["STOCKSTATUSID_Id"] = result;
                                entityInfo[i]["STOCKSTATUSID"] = BasicObject;
                            }

                        }

                    }

                }
            }
        }
        //操作执行前（事务内）事件，在数据检查完毕，启动事务之后触发。
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            //string strSql = string.Format(@"/*dialect*/select A.FSTOCKSTATUSID from t_BD_StockStatus A inner join t_Bd_Stockstatus_l B on A.FSTOCKSTATUSID = B.FSTOCKSTATUSID where B.FNAME = '不参与核算'");
            //long result = DBUtils.ExecuteScalar<long>(base.Context, strSql, -1, null);
            //DynamicObject[] entitys = e.DataEntitys;
            //if (result > 0)
            //{
            //    IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
            //    IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
            //    FormMetadata Meta = metaService.Load(base.Context, "BD_StockStatus") as FormMetadata;//获取基础资料元数据
            //    BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());//不参与核算的库存状态ID
            //}
            //foreach (DynamicObject entity in entitys)
            //{//FSTOCKSTATUSID库存状态  编码  07040000 07040004
            //    if (entity != null)
            //    {

            //        DynamicObjectCollection entityInfo = entity["STK_MISCELLANEOUSENTRY"] as DynamicObjectCollection;
            //        for (int i = 0; i < entityInfo.Count; i++)
            //        {
            //            DynamicObject material = entityInfo[i]["MATERIALID"] as DynamicObject;

            //            if (material["Number"].Equals("07040000") || material["Number"].Equals("07040004"))
            //            {
            //                if (result>0)
            //                {
            //                    string updateStr = string.Format(@"/*dialect*/Update T_STK_MISCELLANEOUSENTRY set FSTOCKSTATUSID = {0} where fentryid = {1}", result, Convert.ToInt64(entityInfo[i]["Id"]));
            //                    DBUtils.Execute(base.Context,updateStr);
            //                }

            //            }

            //        }

            //    }
            //}
        }

    }
}
