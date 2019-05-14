using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.Util;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.NTBZPSTJB
{
    [Description("未按标准配送客户统计表")]
    public class NoTimeSendTable: SysReportBaseService
    {

        #region 参数设置
        private DateTime dtStartDate;
        private DateTime dtEndDate;
        string str0 = string.Empty;
        string instr0 = string.Empty;
        int zq = 0; int jl = 0;



        private string tempTable01 = string.Empty;
        //private string tempTable02 = string.Empty;
        //private string tempTable03 = string.Empty;
        //private string tempTable04 = string.Empty;
        #endregion

        #region 初始化报表参数
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            base.ReportProperty.ReportName = new LocaleValue("未按标准配送客户统计表", this.Context.UserLocale.LCID);   //报表名称
        }
        #endregion

        #region 表列设置
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            header.AddChild("FLINENAME", new LocaleValue("线路", this.Context.UserLocale.LCID));
            header.AddChild("FDELIVIRYROUND", new LocaleValue("配送周期", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
            header.AddChild("FCUSTOMER", new LocaleValue("未按配送周期客户", this.Context.UserLocale.LCID));
            header.AddChild("BZPS", new LocaleValue("标准配送次数", this.Context.UserLocale.LCID));
            header.AddChild("HJPSS", new LocaleValue("按标准配送次数", this.Context.UserLocale.LCID));
            header.AddChild("WPSCS", new LocaleValue("未按标准配送次数", this.Context.UserLocale.LCID));
            //header.AddChild("FWBZ", new LocaleValue("未按标准配送次数", this.Context.UserLocale.LCID));
            //header.AddChild("FBL", new LocaleValue("比例%", this.Context.UserLocale.LCID));
            ListHeader header1 = header.AddChild();
            header1.Caption = new LocaleValue("配送周期", this.Context.UserLocale.LCID);
            for (int i = 0; i < day.Count; i++)
            {
                header1.AddChild(string.Format("f{0}", i), new LocaleValue(day[i], this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            }
            return header;
        }
        #endregion

        #region   实现帐表的主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            int days = (this.dtEndDate.Date - this.dtStartDate.Date).Days + 1;
            string instr1 = string.Format(@"/*dialect*/select/*二开/未按标准配送*/ FCUSTOMERID ,0 fkey,{0} 
                                                from( {1} ) a group by FCUSTOMERID ", str0, instr0);
            zq = Convert.ToInt32(filter.FilterParameter.CustomFilter["F_TL_PSZQ"]);//获取周期
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "l.fname,HJPSS");
            string sqlstr = string.Format(@"/*dialect*/create table {0} as 
            select/*二开/未按标准配送*/ c.fname FCUSTOMER,HJPSS,{6} - HJPSS WPSCS,{6} BZPS,{4} FDELIVIRYROUND,l.fname FLINENAME, a.*, {3}
            from(  
                    select/*二开/未按标准配送*/ FCUSTOMERID,fline,fsaledeptid ,0 fkey,{2}
                    from( {5} ) a group by FCUSTOMERID,fline,fsaledeptid 
                ) a 
            inner join ( select FCUSTOMERID,count(FCUSTOMERID) HJPSS from ( {5} ) group by FCUSTOMERID
            ) a1 on a.fcustomerid=a1.fcustomerid
            join T_BD_CUSTOMER_l c on a.fcustomerid = c.fcustid
            join t_tl_line_l l on l.fid = a.fline where a.fcustomerid in ({1}) 
            ", tableName, GetFCUSTOMERID(filter), str0, KSQL_SEQ, zq, instr0, jl);//
            DBUtils.Execute(this.Context, sqlstr);

        }
        #endregion

        #region  给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            DynamicObject dep = (DynamicObject)filter.FilterParameter.CustomFilter["FSALEDEPTID"];
            string s = dep["Name"].ToString();
            String tableTitle = string.Format(@"{0}---{1} {2}未按照标准周期配送客户统计表", dtStartDate.ToString("yyyy-MM-dd"), dtEndDate.ToString("yyyy-MM-dd"), s);
            ReportTitles titles = new ReportTitles();
            titles.AddTitle("FTITLEFILTER", tableTitle);
            return titles;
        }
        #endregion

        #region  配送周期过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FDELIVIRYROUND(IRptParams filter)
        {

            long deptID = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_TL_PSZQ"]);
            if (deptID == 0)
            {
                return "";
            }
            return string.Format("  and e.FDELIVIRYROUND={0}", deptID);

        }
        #endregion

        #region  部门过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FDeptFilter(IRptParams filter)
        {

            long deptID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEDEPTID_Id"]);
            if (deptID == 0)
            {
                return "";
            }
            return string.Format("and b.FSALEDEPTID={0}", deptID);

        }
        #endregion

        #region  线路过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FLineFilter(IRptParams filter)
        {

            long lineID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FLINE_Id"]);
            if (lineID == 0)
            {
                return "";
            }
            return string.Format("and b.FLINE={0}", lineID);

        }
        #endregion

        #region  过滤未按配送周期配送的客户过滤语句
        /// <summary>
        /// 过滤未按配送周期配送的客户过滤语句
        /// </summary>
        List<string> day = new List<string>();
        private string GetFCUSTOMERID(IRptParams filter)
        {
            day.Clear();
            this.GetFilter(filter);//获取开始、结束日期
            int i, j, k;
            int days = (this.dtEndDate.Date - this.dtStartDate.Date).Days + 1;//日期差
            //转换日期列
            string str = string.Empty;
            double zq2 = Math.Ceiling((double)days / zq);//日期差/周期，全入
            jl = Convert.ToInt32(Math.Ceiling((double)days / zq));
            if (zq * 2 <= days) //选择日期天数大于等于2个配送周期
            {
                str = "sum(case when fdate=to_date('" + FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate) + "','yyyy-mm-dd') and f>0 then '1' else '0' end) as f0";
                day.Add(this.dtEndDate.ToString("yyyy-MM-dd"));
                for (i = 1; i < zq2 * zq; i++)
                {
                    str += " ,sum(case when fdate=to_date('" + FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate.AddDays(-i)) + "','yyyy-mm-dd') and f>0 then '1' else '0' end) as f" + i;
                    day.Add(this.dtEndDate.AddDays(-i).ToString("yyyy-MM-dd"));
                }
            }
            else
            {
                jl = 2;
                str = "sum(case when fdate=to_date('" + FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate) + "','yyyy-mm-dd') and f>0 then '1' else '0' end) as f0";
                day.Add(this.dtEndDate.ToString("yyyy-MM-dd"));
                for (i = 1; i < zq * 2; i++)
                {

                    str += " ,sum(case when fdate=to_date('" + FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate.AddDays(-i)) + "','yyyy-mm-dd') and f>0 then '1' else '0' end) as f" + i;
                    day.Add(this.dtEndDate.AddDays(-i).ToString("yyyy-MM-dd"));
                }
            }

            //日期、销售组织过滤条件
            string rqqj = string.Format(@"and b.FSALEORGID='{0}' and b.FDATE<=to_date('{1}','YYYY-MM-DD') 
                                          and b.FDATE>to_date('{2}','YYYY-MM-DD') {3} {4} {5}",
                          this.Context.CurrentOrganizationInfo.ID, FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate),
                          FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate.AddDays(-i)),
                          FDeptFilter(filter), FDELIVIRYROUND(filter), FLineFilter(filter));
            //原始数据sql
            instr0 = string.Format(@"select fdate, FCUSTOMERID,fline,fsaledeptid,sum(f1) f from (
                                    select b.FDATE,b.FCUSTOMERID,b.fline,b.fsaledeptid,count(b.FCUSTOMERID) f1    
                                    from  T_SAL_OUTSTOCK b join T_BD_CUSTOMER e on b.FCUSTOMERID = e.FCUSTID
                                    where b.fline<>0 and b.fdocumentstatus='C' and b.fvirtual0 = ' ' {0}
                                    group by b.FDATE,b.FCUSTOMERID,b.fline,b.fsaledeptid
                                    union all
                                    select b.FDATE,b.fretcustid,b.fline,b.fsaledeptid,count(b.fretcustid)
                                    from  T_SAL_RETURNSTOCK b join T_BD_CUSTOMER e on b.fretcustid = e.FCUSTID
                                    where b.fline<>0 and b.fdocumentstatus='C' and b.fvirtual0 = ' ' {0}
                                    group by b.FDATE,b.fretcustid,b.fline,b.fsaledeptid  
                                ) group by FDATE,FCUSTOMERID,fline,fsaledeptid order by FDATE", rqqj);
            //按日期列整理
            string instr1 = string.Format(@"/*dialect*/select/*二开/未按标准配送*/ FCUSTOMERID ,0 fkey,{0} 
                                                from( {1} ) a group by FCUSTOMERID ", str, instr0);
            str0 = str;
            DynamicObjectCollection col = DBUtils.ExecuteDynamicObject(this.Context, instr1);

            #region 计算
            List<int> sl = new List<int>();
            int ew = 0;
            for (int num = 0; num < col.LongCount(); num++)//有num条记录
            {
                sl.Clear();
                for (j = 0; j < zq; j++)
                {
                    for (k = j; k < i; k = k + zq)
                    {
                        ew += Convert.ToInt32(col[num]["f" + k]);
                    }
                    sl.Add(ew);
                    ew = 0;
                }
                int pd1 = 0; int pd2 = 0;
                for (int w = 0; w < sl.Count; w++)
                {
                    if (sl[w] != (i / zq) && sl[w] != 0)
                    {
                        break;
                    }
                    else
                    {
                        if (w == 0 && sl.Count == 1)
                        {
                            if (sl[w] == (i / zq))
                            {
                                col[num]["fkey"] = "1";
                            }
                        }
                        else if (w < sl.Count - 1 && sl.Count > 1)
                        {
                            if (sl[w] == (i / zq))
                            {
                                pd1++;
                            }
                            else if (sl[w] == 0)
                            {
                                pd2++;
                            }
                        }
                        else if (w == sl.Count - 1 && sl.Count > 1)
                        {
                            if (sl[w] == (i / zq))
                            {
                                pd1++;
                            }
                            else if (sl[w] == 0)
                            {
                                pd2++;
                            }

                            if (pd1 == 1 && pd2 == (sl.Count - 1))
                            {
                                col[num]["fkey"] = "1";
                            }
                        }
                    }
                }
            }
            #endregion

            string FCUSTOMERID = "";//col中fkey=0的客户即为未按标准配送的客户
            StringBuilder builder = new StringBuilder();
            for (int t = 0; t < col.LongCount(); t++)
            {
                if (0.Equals(Convert.ToInt32(col[t]["fkey"])))
                {
                    FCUSTOMERID += Convert.ToInt32(col[t]["FCUSTOMERID"]) + ",";
                }
            }

            if (!string.IsNullOrWhiteSpace(FCUSTOMERID))
            {
                return FCUSTOMERID.Remove(FCUSTOMERID.Length - 1, 1);
            }
            else
            {
                return "0";
            }

        }
        #endregion

        #region 时间过滤
        protected string GetDataByKey(DynamicObject dy, string key)
        {
            string str = string.Empty;
            if (((dy != null) && (dy[key] != null)) && !string.IsNullOrWhiteSpace(dy[key].ToString()))
            {
                str = dy[key].ToString();
            }
            return str;
        }

        /// <summary>
        /// 获取过滤界面起始日期、截止日期的值
        /// </summary>
        private void GetFilter(IRptParams filter)
        {
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            int num = filter.CustomParams.Count<KeyValuePair<string, object>>();
            this.dtStartDate = this.GetDataByKey(dyFilter, "FSTARTDATE") == string.Empty ? DateTime.MinValue : Convert.ToDateTime(this.GetDataByKey(dyFilter, "FSTARTDATE"));
            this.dtEndDate = this.GetDataByKey(dyFilter, "FENDDATE") == string.Empty ? DateTime.MaxValue : Convert.ToDateTime(this.GetDataByKey(dyFilter, "FENDDATE"));
        }
        #endregion
    }
}
