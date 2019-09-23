using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace KEEPER.K3.TLMB.DistributionStation.BusinessPlugIn
{
    [Description("分销站结算明细表结单后才可以打印")]
    public class EndDeliveryPrintControl:AbstractBillPlugIn
    {
        
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.Equals(""))
            {

            }
        }
    }
}
