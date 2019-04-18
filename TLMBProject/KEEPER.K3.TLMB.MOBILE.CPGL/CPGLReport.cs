using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Mobile.PlugIn;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.MOBILE.CPGL
{
    [Description("产品管理")]
    public class CPGLReport:AbstractMobilePlugin
    {

        private long curCustId;
        public override void AfterCreateModelData(EventArgs e)
        {
            base.AfterCreateModelData(e);
            long userID = base.Context.UserId;
            string strSQL = string.Format(@"select b.fline, c.fname
  from t_sec_user a, t_hr_empinfo b, t_tl_line_l c
 where a.flinkobject = b.fpersonid
   and b.fline = c.fid
   and c.flocaleid = 2052
   and a.fuserid = {0}
", userID);
            DynamicObjectCollection objects = DBUtils.ExecuteDynamicObject(base.Context, strSQL);
            if (objects.Count() > 0)
            {
                this.curCustId = Convert.ToInt64(objects[0]["fline"]);
                base.View.Model.SetValue("FLINEID_Id", objects[0]["fline"]);
                base.View.GetControl("FLINEID").SetCustomPropertyValue("value", objects[0]["fname"]);
            }
            else
            {
                base.View.ShowErrMessage(ResManager.LoadKDString("该用户没有关联员工，请联系管理员", "004113000014336", SubSystemType.SCM, new object[0]), "", MessageBoxType.Notice);
            }
            this.UpdateData();
        }

        private void UpdateData()
        {
            string strSql = string.Format(@"/*dialect*/select /*移动BOS-产品*/
 b.fshortname fname,
 sum(outqty) + sum(fbaseqty) - sum(returnqty0) ftakenum,
 sum(outqty) - sum(returnqty0) outqty,
 sum(fbaseqty) fbaseqty,
 sum(returnqty) returnqty
  from (
        --出库单 
        select a.fmaterialid,
                0             fqty,
                a.frealqty    outqty,
                0             fbaseqty,
                0             returnqty,
                0             returnqty0
          from t_sal_outstockentry a
          join t_sal_outstock b
            on a.fid = b.fid
          join t_bd_customer c
            on b.fcustomerid = c.fcustid
         where b.fline = {0}
           and b.fdate = trunc(sysdate)
           and b.fdocumentstatus = 'C'
        union all
        --及时库存
        select zz.fmaterialid, 0, 0 outqty, zz.fbaseqty, 0 returnqty, 0
          from (select t04.fmaterialid,
                       0,
                       0 outqty,
                       sum(t0.fbaseqty) fbaseqty,
                       0 returnqty,
                       0
                  from t_stk_inventory t0
                  left outer join t_bd_material t04
                    on t0.fmaterialid = t04.fmaterialid
                  left outer join t_bd_materialbase t04base
                    on t04.fmaterialid = t04base.fmaterialid
                  left outer join t_bas_flexvaluesdetail t07
                    on t0.fstocklocid = t07.fid
                  join t_tl_line line
                    on t0.fstockid = line.flinestock
                 where line.fid = {0}
                 group by t04.fmaterialid, t04.fnumber, t0.fstockid) zz
         where zz.fbaseqty > 0
        union all
        --退货
        select a.fmaterialid,
               0,
               0             outqty,
               0             fbaseqty,
               a.frealqty    returnqty,
               0
          from t_sal_returnstockentry a
          join t_sal_returnstock b
            on a.fid = b.fid
         where b.fline = {0}
           and b.freturntype0 = '1'
           and b.fdate = trunc(sysdate)
           and b.fdocumentstatus = 'C'
        union all
        --出库单补差
        select a.fmaterialid,
               0,
               0             outqty,
               0             fbaseqty,
               a.frealqty    returnqty,
               0
          from t_sal_outstockentry a
          join t_sal_outstock b
            on a.fid = b.fid
          join t_bd_customer c
            on b.fcustomerid = c.fcustid
           and c.fkhbd = '1'
         where b.fline = {0}
           and b.fdate = trunc(sysdate)
           and b.fdocumentstatus = 'C'
        union all
        --正品退货
        select a.fmaterialid,
               0,
               0             outqty,
               0             fbaseqty,
               0,
               a.frealqty    returnqty0
          from t_sal_returnstockentry a
          join t_sal_returnstock b
            on a.fid = b.fid
         where b.fline = {0}
           and b.freturntype0 = '2'
           and b.fdate = trunc(sysdate)
           and b.fdocumentstatus = 'C') a
  join t_bd_material_l b
    on a.fmaterialid = b.fmaterialid
  join t_bd_material e
    on e.fmaterialid = b.fmaterialid
 group by b.fshortname
 order by max(e.fno)
",this.curCustId);
            DynamicObjectCollection source = DBUtils.ExecuteDynamicObject(base.Context, strSql);
            if (source.Count()>0)
            {
                DynamicObjectCollection objects2 = this.Model.DataObject["FMobileListViewEntity"] as DynamicObjectCollection;
                objects2.Clear();
                base.View.Model.BeginIniti();
                for (int i = 0; i < source.Count<DynamicObject>(); i++)
                {
                    base.View.Model.CreateNewEntryRow("FMobileListViewEntity");
                    objects2[i]["FNAME"] = source[i]["FNAME"];
                    objects2[i]["FPICKUP"] = source[i]["FTAKENUM"];
                    objects2[i]["FSONG"] = source[i]["OUTQTY"];
                    objects2[i]["FLEFT"] = source[i]["FBASEQTY"];
                    objects2[i]["FRETURN"] = source[i]["RETURNQTY"];
                }
                base.View.Model.EndIniti();
                base.View.UpdateView();
            }
        }
    }
}
