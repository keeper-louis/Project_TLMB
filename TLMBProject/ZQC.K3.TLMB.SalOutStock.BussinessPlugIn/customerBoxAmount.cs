using Kingdee.BOS.Core.Bill.PlugIn;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace ZQC.K3.TLMB.SalOutStock.BussinessPlugIn
{
    [Description("销售出库单子客户计算箱套数量")]
   public class customerBoxAmount : AbstractBillPlugIn
    {
        //根据组织确定其是否是分公司销售，保存前根据物料判断 并且新增行计算箱套数量添加到分录中
        public override void BeforeSave(BeforeSaveEventArgs e)
        {
            base.BeforeSave(e);
            //1.获取单据头使用组织
            DynamicObject useOrg = this.Model.GetValue("FUSEORGID") as DynamicObject;
            long useOrgId = Convert.ToInt64(useOrg["Id"]);
            //通过sql查询该组织下对应所有子客户
            string sql = string.Format(@"select t1.FcustId 
                                           from t_bd_customer t1
                                           where t1.FPRIMARYGROUP=105322  and FUSEORGID = {0}",useOrgId); 

        }
    }
}
