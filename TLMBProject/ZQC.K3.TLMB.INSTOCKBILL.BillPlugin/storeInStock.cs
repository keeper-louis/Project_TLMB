using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.App;

namespace ZQC.K3.TLMB.INSTOCKBILL.BillPlugin
{
    [Description("采购入库单字段携带")]
    //收料部门填写工厂，仓库自动携带该组织工厂成品库
    public class storeInStock : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            string key = e.Field.Key.ToUpperInvariant();
            DynamicObject BasicObject;
            //判断实收数量是否改变
            if ( (key == "FREALQTY" && Convert.ToInt32(e.NewValue) > 0) || (key == "FSTOCKDEPTID" && Convert.ToInt32(e.NewValue) > 0))
            {
                //获取单据组织
                DynamicObject stockOrg = this.Model.GetValue("FSTOCKORGID") as DynamicObject;
                long orgId=Convert.ToInt64(stockOrg["Id"]);
                //获取单据部门（需要判断是否为工厂）
                DynamicObject stockDept = this.Model.GetValue("FSTOCKDEPTID") as DynamicObject;
                long stockDeptId=Convert.ToInt64(stockDept["Id"]);
                //sql 执行sql查询对应组织工厂成品库
                string sql = string.Format(@" select t1.fstockId from t_bd_stock t1 
                                           inner join t_bd_stock_l t2 on t1.fstockid = t2.fstockid
                                           where t2.fname like '%工厂成品库%' and fuseorgid={0}", orgId);
                long result = DBUtils.ExecuteScalar<long>(base.Context, sql, -1, null);
                if (result != -1 && result != 0)
                {
                    IMetaDataService metaService = ServiceHelper.GetService<IMetaDataService>();//元数据服务
                    IViewService view = ServiceHelper.GetService<IViewService>();//界面服务
                    FormMetadata Meta = metaService.Load(base.Context, "BD_STOCK") as FormMetadata;//获取基础资料元数据
                     BasicObject = view.LoadSingle(base.Context, result, Meta.BusinessInfo.GetDynamicObjectType());
                    //sql2 执行sql查询 界面是否为工厂
                    string sql2 = string.Format(@" select t.fdeptid from t_bd_department_l t where t.fname like '%工厂%' ");
                    DynamicObjectCollection result2 = DBUtils.ExecuteDynamicObject(this.Context, sql2);
                    //遍历工厂
                    foreach (DynamicObject stock in result2)
                    {
                        if (stock["FDeptId"].Equals(stockDeptId))
                        {
                            int row = this.Model.GetEntryRowCount("FInStockEntry");
                            for(int i = 0; i < row; i++)
                            {
                                this.Model.SetValue("FStockId", BasicObject, i);
                            }
                            break;
                        }
                        if(!stock["FDeptId"].Equals(stockDeptId))
                        {
                            int row = this.Model.GetEntryRowCount("FInStockEntry");
                            for (int i = 0; i < row; i++)
                            {
                                this.Model.SetValue("FStockId", null, i);
                            }
                        }

                    }



                }


            }
        }
    }
}
            
    

