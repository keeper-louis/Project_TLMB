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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLBM.BOXCAP_CHECKRPT
{
    [Description("箱套日盘点表")]
    public class BoxcapCheckReport: SysReportBaseService
    {
        string tempTable = string.Empty;
        /// <summary>
        /// 初始化数据
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            base.ReportProperty.ReportName = new LocaleValue("箱套日盘点表", this.Context.UserLocale.LCID);   //报表名称
        }
        /// <summary>
        /// 报表动态表头构建
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>

        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            var QY = header.AddChild("F_PAEZ_Date", new LocaleValue("日期"));
            QY.ColIndex = 0;
            var LXQIKC = header.AddChild("F_PAEZ_LXGCFX", new LocaleValue("绿箱&工厂发箱"));
            LXQIKC.ColIndex = 1;
            var LXBQFC = header.AddChild("F_PAEZ_LXFCXS", new LocaleValue("绿箱&返厂箱数"));
            LXBQFC.ColIndex = 2;
            var LXBQFX = header.AddChild("F_PAEZ_LXDXS", new LocaleValue("绿箱&丢箱数"));
            LXBQFX.ColIndex = 3;
            var LXSCDX = header.AddChild("F_PAEZ_LXCE", new LocaleValue("绿箱&差额"));
            LXSCDX.ColIndex = 4;
            var LXBQKC = header.AddChild("F_PAEZ_BXGCFX", new LocaleValue("白箱箱&工厂发箱"));
            LXBQKC.ColIndex = 5;
            var BXQIKC = header.AddChild("F_PAEZ_BXFCXS", new LocaleValue("白箱&返厂箱数"));
            BXQIKC.ColIndex = 6;
            var BXBQFC = header.AddChild("F_PAEZ_BXDXS", new LocaleValue("白箱&丢箱数"));
            BXBQFC.ColIndex = 7;
            var BXBQFX = header.AddChild("F_PAEZ_BXCE", new LocaleValue("白箱&差额"));
            BXBQFX.ColIndex = 8;
            var BXSCDX = header.AddChild("F_PAEZ_GCFXZS", new LocaleValue("工厂发箱总数"));
            BXSCDX.ColIndex = 9;
            var BXBQKC = header.AddChild("F_PAEZ_FCXZS", new LocaleValue("返厂箱总数"));
            BXBQKC.ColIndex = 10;
            var DXZS = header.AddChild("F_PAEZ_DXZS", new LocaleValue("丢箱总数"));
            DXZS.ColIndex = 11;
            var ZCE = header.AddChild("F_PAEZ_ZCE", new LocaleValue("总差额"));
            ZCE.ColIndex = 12;
            return header;
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            insertData();
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as select 1 seq,
       F_PAEZ_Date,
       F_PAEZ_LXGCFX,
       F_PAEZ_LXFCXS,
       F_PAEZ_LXDXS,
       F_PAEZ_LXCE,
       F_PAEZ_BXGCFX,
       F_PAEZ_BXFCXS,
       F_PAEZ_BXDXS,
       F_PAEZ_BXCE,
       F_PAEZ_GCFXZS,
       F_PAEZ_FCXZS,
       F_PAEZ_DXZS,
       F_PAEZ_ZCE,
       {2}
  from {1}", tableName, tempTable, KSQL_SEQ);
            //          string strsql = string.Format(@"/*dialect*/create table {0} as select *,
            //     {2}
            //from {1}", tableName, tempTable, KSQL_SEQ);
            DBUtils.Execute(base.Context, strsql);

            #region ZQC
            //过滤条件
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            //过滤条件   分销站/ka客户
            string kaOrFxz = Convert.ToString(dyFilter["F_PAEZ_RadioGroup"]);
            //过滤条件   获取当前组织
            DynamicObject nowOrg = dyFilter["F_PAEZ_OrgId"] as DynamicObject;
            long nowOrgId = Convert.ToInt64(nowOrg["Id"]);
            //过滤条件 获取发出仓库
            DynamicObject outStock = dyFilter["F_PAEZ_OUTSTOCK"] as DynamicObject;
            long outStockId = Convert.ToInt64(outStock["Id"]);
            //过滤条件 获取分销站
            DynamicObject Fxz=dyFilter["F_PAEZ_Dept"] as DynamicObject;
            long FxzId = Convert.ToInt64(Fxz["Id"]);
            //过滤条件 获取KA客户
            DynamicObject Ka = dyFilter["F_PAEZ_KA"] as DynamicObject;
            long KaId = Convert.ToInt64(Fxz["Id"]);
            //过滤条件 获取返箱仓库
            DynamicObject returnStock = dyFilter["F_PAEZ_InStock"] as DynamicObject;
            long returnStockId = Convert.ToInt64(returnStock["Id"]);
            #region  日期过滤
            //当前日期月份
            DateTime nowDate = Convert.ToDateTime(dyFilter["F_PAEZ_Date"]);
            int nowYear = Convert.ToInt32(nowDate.Year);
            int nowMouth = Convert.ToInt32(nowDate.Month);
            int nowDay = Convert.ToInt32(nowDate.Day);
            string nowYearString = Convert.ToString(nowYear);
            string nowMouthString = Convert.ToString(nowMouth);
            string nowDayString = Convert.ToString(nowDay);
            string nowDateString = nowYearString + "/" + nowMouthString + "/" + nowDayString;
            //上月日期月份
            int lastYear = nowYear - 1;
            int lastMouth;
            string lastYearString;
            string lastMouthString;
            string lastString;
            //所选日期月份
            string selectString;
            selectString = nowYear + "/" + nowMouth + "/" + 20;//选择月第20号
            DateTime last;
            DateTime select = Convert.ToDateTime(selectString);
            //如果月份为1月，年份减一 12月
            if (nowMouth <= 1)
            {
                lastMouth = 12;
                lastYearString = Convert.ToString(lastYear);
                lastMouthString = Convert.ToString(lastMouth);
                lastString = Convert.ToString(lastYearString + "/" + lastMouthString + "/" + 21);
                last = Convert.ToDateTime(lastString);
            }
            else
            {
                lastMouth = nowMouth - 1;
                lastYearString = Convert.ToString(nowYear);
                lastMouthString = Convert.ToString(lastMouth);
                lastString = Convert.ToString(lastYearString + "/" + lastMouthString + "/" + 21);
                last = Convert.ToDateTime(lastString);
            }
            #endregion
            #region   sql编写
            //工厂-分销站
            if (kaOrFxz.Equals("1"))
            {
                # region 工厂发箱
                string sql = string.Format(@"/*dialect*/  select/*直接调拨单*/ d1.FDATE,d1.FSALEDEPTID ,d2.FSRCMATERIALID,sum(d2.FQTY)                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE,d1.FSALEDEPTID,d2.FSRCMATERIALID", lastString,selectString, outStockId,nowOrgId, FxzId);
                DBUtils.Execute(base.Context, sql);
                #endregion
                #region 返厂箱数
                string sql2 = string.Format(@"/*dialect*/  select/*直接调拨单*/ d1.FDATE,d2.FSRCMATERIALID,sum(d2.FQTY)                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调入仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE,d2.FSRCMATERIALID", lastString, selectString, outStockId, nowOrgId, FxzId);
                DBUtils.Execute(base.Context, sql2);
                #endregion




            }
            else if (kaOrFxz.Equals("2"))//工厂-KA客户
            {
                
                #region 工厂发箱
                string sql = string.Format(@"/*dialect*/ 
                                                        select/*销售出库单KA客户*/  s1.FDATE,s2.FMATERIALID,sum(s2.FREALQTY)
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              and s1.FCUSTOMERID={4}
                                                              group by s1.FDATE,s2.FMATERIALID", lastString, selectString, returnStockId, nowOrgId,KaId);
                DBUtils.Execute(base.Context, sql);
                #endregion
                #region 返厂箱数
                string sql2 = string.Format(@"/*dialect*/ 
                                                        select/*销售退货*/  s1.FDATE,s2.FMATERIALID,sum(s2.FREALQTY)
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              and   s1.FRETCUSTID = {4}  /*退货客户=KA客户*/
                                                              group by s1.FRETCUSTID,s2.FMATERIALID,s1.FDATE", lastString,selectString, returnStockId, nowOrgId,KaId);
                DBUtils.Execute(base.Context, sql2);

                #endregion
            }
            #endregion




            #endregion

        }

        private void insertData()
        {
            string strSql = string.Format(@"insert into {0} select 1 seq,
       '2019-10-6' F_PAEZ_Date,
       0 F_PAEZ_LXGCFX,
       0 F_PAEZ_LXFCXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXCE,
       0 F_PAEZ_BXGCFX,
       0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS,
       0 F_PAEZ_DXZS,
       0 F_PAEZ_ZCE
from dual", tempTable);
            DBUtils.Execute(base.Context, strSql);
        }

        private void CreateTempTable()
        {
            this.tempTable = addTempTable(base.Context);
            String strSql = String.Format(@"CREATE TABLE {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               VARCHAR(50),
  F_PAEZ_LXGCFX             VARCHAR(50),
  F_PAEZ_LXFCXS             VARCHAR(50),
  F_PAEZ_LXDXS              VARCHAR(50),
  F_PAEZ_LXCE               VARCHAR(50),
  F_PAEZ_BXGCFX             VARCHAR(50),
  F_PAEZ_BXFCXS             VARCHAR(50),
  F_PAEZ_BXDXS              VARCHAR(50),
  F_PAEZ_BXCE               VARCHAR(50),
  F_PAEZ_GCFXZS             VARCHAR(50),
  F_PAEZ_FCXZS              VARCHAR(50),
  F_PAEZ_DXZS               VARCHAR(50),
  F_PAEZ_ZCE                VARCHAR(50)
)", tempTable);
            DBUtils.Execute(this.Context, strSql);
        }

        private string addTempTable(Context context)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(context);
        }
    }
}
