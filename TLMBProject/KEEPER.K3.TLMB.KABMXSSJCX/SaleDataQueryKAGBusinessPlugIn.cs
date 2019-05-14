using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.CommonFilter.PlugIn;

namespace KEEPER.K3.TLMB.KABMXSSJCX
{
    [Description("KA部门销售数据查询过滤表单插件")]
    public class SaleDataQueryKAGBusinessPlugIn: AbstractCommonFilterPlugIn
    {

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.ToUpper().Equals("FSALEDEPTID"))
            {
                DynamicObject saleCompany = this.Model.GetValue("FSaleCompanyId") as DynamicObject;
                if (saleCompany==null)
                {
                    return;
                }
                string companyNumber = Convert.ToString(saleCompany["Number"]).Substring(0, 5);
                string filter = string.Format(" FNUMBER like '{0}%' AND FDEPTTYPE = '2'  ", companyNumber);
                if (string.IsNullOrEmpty(e.ListFilterParameter.Filter))
                {
                    e.ListFilterParameter.Filter = filter;
                }
                else
                {
                    filter = " And " + filter;
                    e.ListFilterParameter.Filter += filter;
                }

            }
            
        }
    }
}
