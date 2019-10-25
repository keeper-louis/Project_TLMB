using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace KEEPER.K3.TLMB.StockElse.BussinessPlugIn
{
    [Description("保存前刷新箱套物料库存状态为不参与核算")]
    public class beforeSave:AbstractBillPlugIn
    {
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            int count = this.Model.GetEntryRowCount("FEntity");
            for (int i = 0; i < count; i++)
            {
                if (Convert.ToString(((DynamicObject)this.Model.GetValue("FMATERIALID", i))["Number"]).Equals("07040000") || Convert.ToString(((DynamicObject)this.Model.GetValue("FMATERIALID", i))["Number"]).Equals("07040004"))
                {
                    this.Model.SetItemValueByNumber("FSTOCKSTATUSID", "KCZT001", i);
                }
            }
        }
    }
}
