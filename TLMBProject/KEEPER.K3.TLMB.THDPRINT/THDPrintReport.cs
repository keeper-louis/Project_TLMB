using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.THDPRINT
{
    [Description("提货单打印（物料分组）")]
    public class THDPrintReport:SysReportBaseService
    {
        #region 参数设置
        private string tempTable00 = string.Empty;
        private DateTime date0;
        //private CommonFunction commonFuction = new CommonFunction();
        #endregion
        #region 初始化报表参数
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            this.ReportProperty.ReportType = Kingdee.BOS.Core.Report.ReportType.REPORTTYPE_MOVE;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            // base.ReportProperty.ReportName = new LocaleValue("客户对账单", this.Context.UserLocale.LCID);   //报表名称
        }
        #endregion

        private void CreateTempTalbe(IRptParams filter)
        {
            this.tempTable00 = ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(this.Context);

            string sqlstr0 = string.Format(@"create table {0}
                                            (fid decimal(23,10)
                                            ,fmaterialid decimal(23,10)
                                            ,FJITQTY decimal(23,10)
                                             )", tempTable00);
            DBUtils.Execute(this.Context, sqlstr0);
        }

        /// <summary>
        /// 构建分页报表每个报表的临时表
        /// 首先从分页依据中拿到本次分页的条件，就是当前页报表的条件：this.CacheDataList[filter.CurrentPosition]
        /// 然后把条件拼装到SQL中，例如b.FLocaleId= dr["FLocaleId"] 语言id=当前报表的语言id
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="tableName"></param>

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.date0 = Convert.ToDateTime(filter.FilterParameter.CustomFilter["FDate"]);
            long orgId = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEORGID_Id"]);
            long deptId = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEDEPTID_Id"]);
            string date = date0.ToString("d");
            string date1 = date0.AddDays(-1).ToString("d");
            string time = string.Format(@" and FDATE >= trunc(to_date('{0}', 'YYYY/MM/DD'), 'mm') and FDATE <= to_date('{0}', 'YYYY/MM/DD')", date1);
            DataRow dr = this.CacheDataList[filter.CurrentPosition];

            this.CreateTempTalbe(filter);
            string sqlstr0 = string.Format(@"/*dialect*/insert into {0}(fid,fmaterialid,FJITQTY)
                select /*二开/提货单and c.fid='{1}'*/ a.fid,a.fmaterialid,sum(fqty)-sum(fretrunqty) FJITQTY from (
                --销售出库
                SELECT  'XSCK' RS ,a.fdate,c.fid,b.fmaterialid,0 fqty,sum(b.frealqty) fretrunqty FROM T_SAL_OUTSTOCK A,T_SAL_OUTSTOCKENTRY B,t_tl_line c 
                WHERE A.FID=B.FID and c.flinestock=b.fstockid and a.fdocumentstatus='C' and a.FCANCELSTATUS='A' and a.fline='{1}' {2}
                group by a.fdate, c.fid,b.fmaterialid
                union all
                --销售退货单--正品
                SELECT  'ZPTH' RS,a.fdate, c.fid,b.fmaterialid,sum(b.frealqty),0  FROM t_Sal_Returnstock A,t_Sal_ReturnstockENTRY B,t_tl_line c 
                WHERE A.FID=B.FID  and a.fdocumentstatus='C' and a.FCANCELSTATUS='A' and c.flinestock=b.fstockid and c.fid='{1}' {2}
                group by a.fdate,c.fid,b.fmaterialid
                union all
                ---调拨入
                select 'DBR'RS,a.fdate,c.fid,b.fmaterialid,sum(b.fqty),0  from T_STK_STKTRANSFERIN a, T_STK_STKTRANSFERINENTRY B,t_tl_line c
                WHERE A.FID=B.FID and a.fdocumentstatus='C' and c.flinestock=b.FDESTSTOCKID and c.fid='{1}' {2}
                group by    a.fdate,c.fid,b.fmaterialid
                union all
                ---调拨出
                select 'DBC'RS, a.fdate,c.fid,b.fmaterialid,0,sum(b.fqty)  from T_STK_STKTRANSFERIN a, T_STK_STKTRANSFERINENTRY B,t_tl_line c
                 WHERE A.FID=B.FID and a.fdocumentstatus='C' and c.flinestock=b.FSRCSTOCKID and c.fid='{1}' {2}
                group by a.fdate,c.fid,b.fmaterialid ) a group by a.fid,a.fmaterialid", tempTable00, dr["fid"].ToString(), time);
            DBUtils.Execute(this.Context, sqlstr0);


            string sSQL = @"select /*二开/提货单*/ a.*,{0} into {1} from (
select  spp.fno FSEQ,spp.fnumber Fmaterialid,ml.Fshortname,ml.fmaterialid fmid, spp.FPACKQTY ,FPACKERQTY , FQTY, FJITQTY  
from  t_bd_material spp 
 left join 
(
 select fpickdate,fsalerid,fmaterialid, sum(FQTY) FQTY,sum(FPACKERQTY) FPACKERQTY from T_STK_STKTRANSFERIn tss
join  t_Stk_Stktransferinentry tse  on tss.fid=tse.fid 
LEFT join v_bd_salesman   vs on   tss.fsalerid=vs.FID
left join t_hr_empinfo he on he.fstaffid=vs.fstaffid 
where he.fline=" + dr["fid"].ToString() + @" and tss.fid=tse.fid   and  fsaledeptid={2} and fpickdate= to_date('{3}','YYYY/MM/DD') AND fsalerid<>0  
group by fpickdate,fsalerid,fmaterialid) sss on spp.fmaterialid=sss.fmaterialid    
left join {5} sst on sst.FMATERIALID=spp.fmaterialid left join t_bd_material_l ml on ml.FMATERIALID=spp.FMATERIALID and FLocaleId=2052   
where spp.fno>0 and spp.FPACKQTY>0 and spp.fdocumentstatus='C'  and (sss.FQTY>0 or FPACKERQTY>0 or FJITQTY>0)  and spp.fnumber like '0700%' and  spp.fuseorgid={4}" +
            ") a inner join t_bd_material h on h.fmaterialid=a.fmid {6} ";
            KSQL_SEQ = string.Format(KSQL_SEQ, "a.FSEQ");
            sSQL = string.Format(sSQL, this.KSQL_SEQ, tableName, deptId, date, orgId, tempTable00, FWLFilter(filter));
            //Convert.ToInt64(filter.FilterParameter.CustomFilter["FFilterOrgId_Id"]            
            DBUtils.Execute(this.Context, sSQL);
            this.DropTable();
        }

        private void DropTable()
        {
            string delsql1 = string.Format("TRUNCATE TABLE {0}", tempTable00);
            DBUtils.Execute(this.Context, delsql1);
            string dropsql1 = string.Format("DROP TABLE {0}", tempTable00);
            DBUtils.Execute(this.Context, dropsql1);
        }

        public override List<Kingdee.BOS.Core.Report.SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<Kingdee.BOS.Core.Report.SummaryField> fls = new List<Kingdee.BOS.Core.Report.SummaryField>();
            Kingdee.BOS.Core.Report.SummaryField fs = new Kingdee.BOS.Core.Report.SummaryField("FQTY", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM);
            Kingdee.BOS.Core.Report.SummaryField fs0 = new Kingdee.BOS.Core.Report.SummaryField("FJITQTY", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM);
            Kingdee.BOS.Core.Report.SummaryField fs1 = new Kingdee.BOS.Core.Report.SummaryField("FPACKERQTY", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM);
            fls.Add(fs);
            fls.Add(fs0);
            fls.Add(fs1);
            return fls;
        }


        /// <summary>
        /// 分页报表必须实现的方法，此方法用于为报表提供分页依据。
        /// 比如以下示例：分别按语言来对部门分类，也就是说每种语言一个报表，中文的是一个报表、英文的一个报表，繁体的一个
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override DataTable GetList(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            DynamicObjectCollection lineId = dyFilter["FLINEID"] as DynamicObjectCollection;
            DataTable dt;
            string sSQL = @"select  /*二开/提货单*/b.fname FLNAME, a.fid , c.fname FSNAME, e.fname FENAME  from t_tl_line a, t_tl_line_l b,t_bd_stock_l c,t_hr_empinfo d  ,t_hr_empinfo_l e
 where a.fid=b.fid  and c.fstockid=a.flinestock and  d.fline=a.fid and d.fid=e.fid  and  fdept={0} and d.FFORBIDSTATUS='A'
";
            string sSQL1 = @"  AND a.fid in {1}";
            string sSQL2 = @" order by  b.fname";
            if (lineId.Count > 0)
            {
                sSQL = string.Format(sSQL + sSQL1 + sSQL2, Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEDEPTID_Id"]), this.getLineId(filter));
            }
            else
            {
                sSQL = string.Format(sSQL + sSQL2, Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEDEPTID_Id"]), this.getLineId(filter));
            }
            dt = DBUtils.ExecuteDataSet(this.Context, sSQL).Tables[0];
            return dt;
        }

        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles titles = new ReportTitles();
            if (CacheDataList == null)
            {
                DataTable dt = GetList(filter);
                if (dt != null && dt.Rows.Count > 0)
                {
                    titles.AddTitle("FLINE", dt.Rows[0]["FLNAME"].ToString());
                    return titles;
                }
                return null;
            }
            try
            {
                DataRow dr = this.CacheDataList[filter.CurrentPosition];
                titles.AddTitle("FLINE", dr["FLNAME"].ToString());
                titles.AddTitle("FSNAME", dr["FSNAME"].ToString());
                titles.AddTitle("FENAME", dr["FENAME"].ToString());
                titles.AddTitle("FDATE", date0.ToString("d"));
                return titles;
            }
            catch
            {

                throw new KDException("操作异常", "线路无销售员！！！");
            }
        }
        private string getLineId(IRptParams filter)
        {
            DynamicObjectCollection collection = filter.FilterParameter.CustomFilter["FLINEID"] as DynamicObjectCollection;
            if (collection == null || collection.Count <= 0)
            {
                return "";
            }

            List<long> cusIds = collection.Select(o => Convert.ToInt64(o["FLINEID_Id"])).ToList();

            return string.Format("({0})", string.Join(",", cusIds));
        }

        #region  物料分组过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>

        private string FWLFilter(IRptParams filter)
        {

            string s = Convert.ToString(filter.FilterParameter.CustomFilter["FMATERIALGROUP"]);

            if (s == "")
            {
                return "";
            }
            return string.Format("and h.FWLFL='{0}'", s);

        }
        #endregion

        #region 表列设置
        //sp.fseq, SP.FMATERIALID,kk.fqty,kk.fjitqty
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            header.AddChild("FSEQ", new LocaleValue("序号", this.Context.UserLocale.LCID));
            header.AddChild("Fmaterialid", new LocaleValue("物料代码", this.Context.UserLocale.LCID));
            header.AddChild("Fshortname", new LocaleValue("物料名称", this.Context.UserLocale.LCID));
            header.AddChild("FPACKQTY", new LocaleValue("包装数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("FPACKERQTY", new LocaleValue("装箱数", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("FQTY", new LocaleValue("报货数", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("FJITQTY", new LocaleValue("昨日剩货数", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            return header;
        }
        #endregion
    }
}
