using KEEPER.K3.TLMB.ServiceHelper;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.CUSTOMER.ServicePlugIn
{
    [Description("客户禁用审批单反审核自动反禁用对应客户")]
    public class UnAudit:AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            e.FieldKeys.Add("FCUSTID");
        }
        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            base.EndOperationTransaction(e);
            if (e.DataEntitys != null && e.DataEntitys.Count<DynamicObject>() > 0)
            {
                foreach (DynamicObject item in e.DataEntitys)
                {
                    string custId = Convert.ToString(item["FCUSTID_ID"]);
                    string[] pkIds = new string[] { custId };
                    TLMBServiceHelper.SetState(this.Context, "BD_Customer", pkIds, "FCUSTID", "T_BD_CUSTOMER", "FForbidStatus", "A");
                }
            }
        }
    }
}
