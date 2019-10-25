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
            //判断实收数量是否改变
            if(key == "FREALQTY" && Convert.ToInt32(e.NewValue) > 0)
            {
                            DynamicObject stockDept = this.Model.GetValue("FSTOCKDEPTID") as DynamicObject;
                if(stockDept!=null){

            
                        //进行回传
                            if (stockDept["Number"].Equals("04021000") && stockDept["Number"] != null)
                            {
                               this.Model.SetItemValueByNumber("FStockId", "04CK01001",e.Row);
                            }
                }
            }
            else if(key == "FSTOCKDEPTID" && Convert.ToString(e.NewValue) != "0402100")
            {
                DynamicObject stockDept = this.Model.GetValue("FSTOCKDEPTID") as DynamicObject;
                if(stockDept["Number"].Equals("04021000") && stockDept["Number"] != null)
                {
                    int row = this.Model.GetEntryRowCount("FInStockEntry");
                    for (int i = 0; i < row; i++)
                    {
                        //DynamicObject stockId = this.Model.GetValue("FSTOCKID") as DynamicObject;
                        this.Model.SetItemValueByNumber("FStockId", "04CK01001", i);
                    }
                }
                else if(stockDept["Number"]!= "04021000")
                { 
                int row=this.Model.GetEntryRowCount("FInStockEntry");
                for (int i=0;i<row;i++)
                {
                    //DynamicObject stockId = this.Model.GetValue("FSTOCKID") as DynamicObject;
                    this.Model.SetValue("FStockId", null,i);
                }
                }

            }
            //else if(key== "FSTOCKDEPTID" && Convert.ToInt32(e.NewValue).Equals("0402100"))
            //{
            //    int row = this.Model.GetEntryRowCount("FInStockEntry");
            //    for (int i = 0; i < row; i++)
            //    {
            //        //DynamicObject stockId = this.Model.GetValue("FSTOCKID") as DynamicObject;
            //        this.Model.SetItemValueByNumber("FStockId", "04CK01001", i);
            //    }
            //}
        }
    }
}
            
    



//int count = this.Model.GetEntryRowCount("FEntity");
//            for (int i = 0; i<count; i++)
//            {
//                if (Convert.ToString(((DynamicObject)this.Model.GetValue("FMATERIALID", i))["Number"]).Equals("07040000") || Convert.ToString(((DynamicObject)this.Model.GetValue("FMATERIALID", i))["Number"]).Equals("07040004"))
//                {
//    this.Model.SetItemValueByNumber("FSTOCKSTATUSID", "KCZT001", i);
//}
//}
//        }


//04CK01001 工厂成品库编码

//public override void AfterLoadData(EventArgs e)
//{
//    base.AfterLoadData(e);
//    var value = this.Model.GetValue("StockDeptId");
//    if (value == null || !value.Equals("04021000"))
//    {
//        this.Model.SetValue("StockDeptId", "04021000");
//    }
//}