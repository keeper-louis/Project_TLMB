﻿using Kingdee.BOS.Core.DynamicForm.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.App;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.DynamicForm;
using KEEPER.K3.TLMB.ServiceHelper;

namespace KEEPER.K3.TLMB.CUSTOMER.ServicePlugIn
{
    [Description("客户禁用审批单审核自动禁用对应客户")]
    public class Audit:AbstractOperationServicePlugIn
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
                    TLMBServiceHelper.SetState(this.Context, "BD_Customer", pkIds, "FCUSTID", "T_BD_CUSTOMER", "FForbidStatus", "B");
                }
            }
        }
    }
}
