﻿using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using KEEPER.K3.TLMB.ServiceHelper;

namespace KEEPER.K3.TLMB.SalOutStock.BusinessPlugIn
{
    [Description("销售出库单表单插件")]
    public class SalOutStockBill: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            String key = e.Field.Key.ToUpperInvariant();
            long custId;
            long orgid;
            //实发数量发生变化，判断是否是赠品，如果是赠品取赠品价格
            if (key == "FREALQTY" && Convert.ToInt32(e.NewValue) != 0)
            {
                DynamicObject SalOrg = this.Model.GetValue("FSALEORGID") as DynamicObject;
                DynamicObject CustObject = this.Model.GetValue("FCustomerID") as DynamicObject;
                if (CustObject!=null && SalOrg!=null)
                {
                     custId = Convert.ToInt64(CustObject["id"]);
                     orgid = Convert.ToInt64(SalOrg["id"]);
                }
                Dictionary<int, double> priceDictionary = TLMBServiceHelper.GetPriceDictionary(this.Context, Convert.ToDateTime(this.Model.GetValue("FDate")).ToShortDateString(), custId, orgid);
                if (this.Model.GetValue("FISTASTE", e.Row) != null && this.Model.GetValue("FISTASTE", e.Row).ToString() != "" && this.Model.GetValue("FISTASTE", e.Row).ToString() != " ")
                {
                    DynamicObject MaterObject1 = this.Model.GetValue("FMaterialId", e.Row) as DynamicObject;
                    int FMaterialId = Convert.ToInt32(MaterObject1["id"]);
                    double price = priceDictionary[FMaterialId];
                    this.Model.SetValue("FTAXPRICE", price, e.Row);
                    this.Model.SetValue("FPRICE", price / 1.16, e.Row);
                }
            }
        }
    }
}
