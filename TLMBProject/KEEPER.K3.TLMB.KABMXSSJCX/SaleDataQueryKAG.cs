using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
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

namespace KEEPER.K3.TLMB.KABMXSSJCX
{
    [Description("KA部门销售数据查询")]
    public class SaleDataQueryKAG: SysReportBaseService
    {

        #region 参数设置
        private DateTime dtStartDate;
        private DateTime dtEndDate;

        long columnFilter = 0;

        private string tempTable002 = string.Empty;
        private string tempTable01 = string.Empty;
        private string tempTable02 = string.Empty;
        private string tempTable03 = string.Empty;
        private string tempTable04 = string.Empty;
        #endregion

        #region 初始化报表参数
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            base.ReportProperty.ReportName = new LocaleValue("销售数据查询", this.Context.UserLocale.LCID);   //报表名称
        }
        #endregion

        #region 表列设置
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            //int columnFilter = Convert.ToInt32(filter.FilterParameter.CustomFilter["F_TL_Combo"]);
            ReportHeader header = new ReportHeader();
            //header.AddChild("FNUM1", new LocaleValue("序号", this.Context.UserLocale.LCID));
            header.AddChild("FNAME", new LocaleValue(ColumnShowFilter(filter), this.Context.UserLocale.LCID));

            ListHeader header2 = header.AddChild();
            header2.Caption = new LocaleValue("汇总", this.Context.UserLocale.LCID);

            ListHeader header3 = header.AddChild();
            header3.Caption = new LocaleValue("去年同期", this.Context.UserLocale.LCID);

            if ((columnFilter == 2) || (columnFilter == 3) || (columnFilter == 4))
            {
                header2.AddChild("FSTOCKNUM", new LocaleValue("进货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
                header2.AddChild("FSALENUM", new LocaleValue("销售数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
                header2.AddChild("FRETURNNUM", new LocaleValue("返货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);

                header3.AddChild("FSTOCKNUM_LAST", new LocaleValue("进货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
                header3.AddChild("FSALENUM_LAST", new LocaleValue("销售数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
                header3.AddChild("FRETURNNUM_LAST", new LocaleValue("返货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlInt);
            }
            header2.AddChild("FSTOCKQUOTA", new LocaleValue("进货额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header2.AddChild("FSALEQUOTA", new LocaleValue("销售额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header2.AddChild("FRETURNQUOTA", new LocaleValue("返货额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header2.AddChild("FSALEPER", new LocaleValue("销售占比", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header2.AddChild("FRETURNPER", new LocaleValue("返货率", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

            header3.AddChild("FSTOCKQUOTA_LAST", new LocaleValue("进货额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header3.AddChild("FSALEQUOTA_LAST", new LocaleValue("销售额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header3.AddChild("FRETURNQUOTA_LAST", new LocaleValue("返货额", this.Context.UserLocale.LCID), SqlStorageType.SqlMoney);
            header3.AddChild("FSALEPER_LAST", new LocaleValue("销售占比", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header3.AddChild("FRETURNPER_LAST", new LocaleValue("返货率", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

            header.AddChild("FSALEQUOTAPER", new LocaleValue("销售额同比成长", this.Context.UserLocale.LCID));

            return header;
        }
        #endregion

        #region   实现帐表的主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            columnFilter = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_TL_Combo"]);
            this.CreateTempTalbe();
            this.InsertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "rownum");
            string s1 = string.Empty;
            if (columnFilter == 2 || columnFilter == 4)
            {
                s1 = string.Format("inner join t_bd_material b on b.fmaterialid=a.fid order by b.fno");
            }
            else
            {
                //s1 = "where a.FNAME is not null ORDER BY a.FNAME";
                s1 = "ORDER BY a.FNAME";
            }
            string sqlstr = string.Format(@"/*dialect*/
            create table {0} as 
            select /*二开/KA部门*/rownum FNUM1,a.*, {2}
            from(
                select a.*, rownum FNUM from(
                select a.FNAME,a.FID, FSTOCKNUM, FSALENUM, FRETURNNUM, FSTOCKQUOTA, FSALEQUOTA, FRETURNQUOTA, FSALEPER, FRETURNPER,
                                FSTOCKNUM_LAST, FSALENUM_LAST, FRETURNNUM_LAST, FSTOCKQUOTA_LAST, FSALEQUOTA_LAST, FRETURNQUOTA_LAST, 
                                FSALEPER_LAST, FRETURNPER_LAST,FSALEQUOTAPER
                from {1} a {3}) a
                union all
                select '合计' FNAME,' ' fid, sum(FSTOCKNUM) FSTOCKNUM, sum(FSALENUM) FSALENUM, sum(FRETURNNUM) FRETURNNUM, 
                                             sum(FSTOCKQUOTA) FSTOCKQUOTA, sum(FSALEQUOTA) FSALEQUOTA, sum(FRETURNQUOTA) FRETURNQUOTA, '' FSALEPER,
                                    (case when sum(FSTOCKQUOTA) <> 0 then
                                    to_char((sum(FRETURNQUOTA)/sum(FSTOCKQUOTA)) * 100,'99990.00') || '%'
                                    else to_char(0.00) || '%' end) FRETURNPER,
                                        sum(FSTOCKNUM_LAST) FSTOCKNUM_LAST, sum(FSALENUM_LAST) FSALENUM_LAST, sum(FRETURNNUM_LAST) FRETURNNUM_LAST, 
                                        sum(FSTOCKQUOTA_LAST) FSTOCKQUOTA_LAST, sum(FSALEQUOTA_LAST) FSALEQUOTA_LAST, sum(FRETURNQUOTA_LAST) FRETURNQUOTA_LAST, '' FSALEPER_LAST,
                                    (case when sum(FSTOCKQUOTA_LAST) <> 0 then
                                    to_char((sum(FRETURNQUOTA_LAST)/sum(FSTOCKQUOTA_LAST)) * 100,'99990.00') || '%'
                                    else to_char(0.00) || '%' end) FRETURNPER_LAST,
                                    (case when sum(FSALEQUOTA_LAST) <> 0 then
                                    to_char(((sum(FSALEQUOTA) - sum(FSALEQUOTA_LAST))/sum(FSALEQUOTA_LAST)) * 100,'99990.00') || '%'
                                    else to_char(0.00) || '%' end) FSALEQUOTAPER, 99999999 FNUM from {1}
            ) a ", tableName, tempTable04, base.KSQL_SEQ, s1);
            DBUtils.Execute(this.Context, sqlstr);
            this.DropTable();
        }
        #endregion

        #region 创建临时表

        /// <summary>
        /// 给临时表赋表名
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string AddTempTable(Context ctx)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(ctx);
        }


        private void CreateTempTalbe()
        {
            this.tempTable002 = AddTempTable(base.Context);
            this.tempTable01 = AddTempTable(base.Context);
            this.tempTable02 = AddTempTable(base.Context);
            this.tempTable03 = AddTempTable(base.Context);
            this.tempTable04 = AddTempTable(base.Context);

            string sqlstr02 = string.Format(@"create table {0}
                                            (fmasterid varchar(1000)
                                            ,FNAME varchar(1000)
                                             )", tempTable002);
            DBUtils.Execute(this.Context, sqlstr02);

            string sqlstr1 = string.Format(@"create table {0}
                                            (FSALEDEPTID varchar(200)
                                            ,FSALEORGID varchar(200)
                                            ,fforgroupcust varchar(200)
                                            ,FCUSTID varchar(200)
                                            ,FMATERIALID varchar(200)
                                            ,FSTOCKNUM decimal(23,10)
                                            ,FSTOCKQUOTA decimal(23,10)
                                            ,FRETURNNUM decimal(23,10)
                                            ,FRETURNQUOTA decimal(23,10)
                                             )", tempTable01);
            DBUtils.Execute(this.Context, sqlstr1);

            string sqlstr2 = string.Format(@"create table {0}
                                            (FSALEDEPTID varchar(200)
                                            ,FSALEORGID varchar(200)
                                            ,fforgroupcust varchar(200)
                                            ,FCUSTID varchar(200)
                                            ,FMATERIALID varchar(200)
                                            ,FSTOCKNUM decimal(23,10)
                                            ,FSTOCKQUOTA decimal(23,10)
                                            ,FRETURNNUM decimal(23,10)
                                            ,FRETURNQUOTA decimal(23,10)
                                             )", tempTable02);
            DBUtils.Execute(this.Context, sqlstr2);

            string sqlstr3 = string.Format(@"create table {0}
                                            (FNAME varchar(1000)
                                            ,FID varchar(1000)
                                            ,FSTOCKNUM decimal(23,10)
                                            ,FSTOCKQUOTA decimal(23,10)
                                            ,FRETURNNUM decimal(23,10)
                                            ,FRETURNQUOTA decimal(23,10)
                                            ,FSALENUM decimal(23,10)
                                            ,FSALEQUOTA decimal(23,10)
                                            ,FSTOCKNUM_LAST decimal(23,10)
                                            ,FSTOCKQUOTA_LAST decimal(23,10)
                                            ,FRETURNNUM_LAST decimal(23,10)
                                            ,FRETURNQUOTA_LAST decimal(23,10)
                                            ,FSALENUM_LAST decimal(23,10)
                                            ,FSALEQUOTA_LAST decimal(23,10)
                                             )", tempTable03);
            DBUtils.Execute(this.Context, sqlstr3);

            string sqlstr4 = string.Format(@"create table {0}
                                            (FNAME varchar(1000)
                                            ,FID varchar(1000)
                                            ,FSTOCKNUM decimal(23,10)
                                            ,FSALENUM decimal(23,10)
                                            ,FRETURNNUM decimal(23,10)
                                            ,FSTOCKQUOTA decimal(23,10)
                                            ,FSALEQUOTA decimal(23,10)
                                            ,FRETURNQUOTA decimal(23,10)
                                            ,FSALEPER varchar(1000)
                                            ,FRETURNPER varchar(1000)
                                            ,FSTOCKNUM_LAST decimal(23,10)
                                            ,FSALENUM_LAST decimal(23,10)
                                            ,FRETURNNUM_LAST decimal(23,10)
                                            ,FSTOCKQUOTA_LAST decimal(23,10)
                                            ,FSALEQUOTA_LAST decimal(23,10)
                                            ,FRETURNQUOTA_LAST decimal(23,10)
                                            ,FSALEPER_LAST varchar(1000)
                                            ,FRETURNPER_LAST varchar(1000)
                                            ,FSALEQUOTAPER varchar(1000)
                                             )", tempTable04);
            DBUtils.Execute(this.Context, sqlstr4);
        }
        #endregion

        #region 写入数据
        private void InsertData(IRptParams filter)
        {
            #region 获取连锁信息
            string instr02 = string.Format(@"/*dialect*/
                                insert into {0}(fmasterid, FNAME)
                                select fmasterid,FDATAVALUE FNAME
                                from (select fmasterid,FDATAVALUE from T_BAS_ASSISTANTDATAENTRY de 
                                inner join T_BAS_ASSISTANTDATAENTRY_L del on del.fentryid=de.fentryid 
                                and de.fid='57f6f7d7ae19c4')", tempTable002);
            DBUtils.Execute(this.Context, instr02);
            #endregion

            #region KA部门查询数据
            string instr0 = @"/*dialect*/
                    insert into {0}(FSALEDEPTID, FSALEORGID,fforgroupcust,FCUSTID, FMATERIALID, FSTOCKNUM, FSTOCKQUOTA, FRETURNNUM, FRETURNQUOTA)
                    select/*二开/KA部门*/ FSALEDEPTID,FSALEORGID,fforgroupcust ,FCUSTID, FMATERIALID,
                            (FSTOCKNUM-FRETURNNUM_Z) FSTOCKNUM, (FSTOCKQUOTA-FRETURNQUOTA_Z) FSTOCKQUOTA, 
                            (FRETURNNUM-FRETURNNUM_Z) FRETURNNUM,(FRETURNQUOTA-FRETURNQUOTA_Z) FRETURNQUOTA
                    from (
                        select e.FSSKAZ FSALEDEPTID,b.FSALEORGID,e.fforgroupcust,b.FCUSTOMERID FCUSTID,a.FMATERIALID,
                                a.FREALQTY FSTOCKNUM, a.FREALQTY * c.FTAXPRICE FSTOCKQUOTA,0 FRETURNNUM, 0 FRETURNQUOTA,0 FRETURNNUM_Z,0 FRETURNQUOTA_Z
                                from T_SAL_OUTSTOCKENTRY a join T_SAL_OUTSTOCK b on a.FID = b.FID join T_SAL_OUTSTOCKENTRY_F c on a.FENTRYID = c.FENTRYID   
                                join T_BD_CUSTOMER e on b.FCUSTOMERID = e.FCUSTID
                                join T_BD_DEPARTMENT d on e.FSSKAZ = d.FDEPTID
                                join T_BD_MATERIAL h on a.FMATERIALID = h.FMATERIALID
                                where 1=1 {1} {2} {3} {4} {5} {6} {8} {9} and b.fdocumentstatus='C' and b.FSALEORGID='{7}'
                        union all
                        select e.FSSKAZ FSALEDEPTID,b.FSALEORGID,e.fforgroupcust,b.FRETCUSTID FCUSTID,a.FMATERIALID,
                                0 FSTOCKNUM, 0 FSTOCKQUOTA,a.FREALQTY FRETURNNUM, a.FREALQTY * c.FTAXPRICE FRETURNQUOTA,0 FRETURNNUM_Z,0 FRETURNQUOTA_Z
                                from T_SAL_RETURNSTOCKENTRY a join T_SAL_RETURNSTOCK b on a.FID = b.FID join T_SAL_RETURNSTOCKENTRY_F c on a.FENTRYID = c.FENTRYID
                                join T_BD_CUSTOMER e on b.FRETCUSTID = e.FCUSTID
                                join T_BD_DEPARTMENT d on e.FSSKAZ = d.FDEPTID
                                join T_BD_MATERIAL h on a.FMATERIALID = h.FMATERIALID
                                where 1=1 {1} {2} {3} {4} {5} {6} {9} and b.fdocumentstatus='C' and b.FSALEORGID='{7}'
                        union all
                        select e.FSSKAZ FSALEDEPTID,b.FSALEORGID,e.fforgroupcust,b.FRETCUSTID FCUSTID,a.FMATERIALID,
                                0 FSTOCKNUM, 0 FSTOCKQUOTA, 0 FRETURNNUM, 0 FRETURNQUOTA,a.FREALQTY FRETURNNUM_Z,a.FREALQTY * c.FTAXPRICE FRETURNQUOTA_Z
                                from T_SAL_RETURNSTOCKENTRY a join T_SAL_RETURNSTOCK b on a.FID = b.FID join T_SAL_RETURNSTOCKENTRY_F c on a.FENTRYID = c.FENTRYID
                                join T_BD_CUSTOMER e on b.FRETCUSTID = e.FCUSTID
                                join T_BD_DEPARTMENT d on e.FSSKAZ = d.FDEPTID
                                join T_BD_MATERIAL h on a.FMATERIALID = h.FMATERIALID
                                where 1=1 {1} {2} {3} {4} {5} {6} {9} and b.fdocumentstatus='C' and b.FSALEORGID='{7}' and b.FRETURNTYPE0='2'
                    )";
            #endregion

            #region KA部门今年、去年销售数据
            //获取过滤条件值
            string FDept = FDeptFilter(filter); string FMaterial = FMaterialIDFilter(filter); string FWL = FWLFilter(filter);
            string FGroup = FGroupFilter(filter); string FChain = FChainFilter(filter); long FOrg = this.Context.CurrentOrganizationInfo.ID;
            string FSFZP = FSFZPFilter(filter);string FSaleCompany = FDeptCompanyFilter(filter);
            //今年
            string instr1 = string.Format(instr0, tempTable01, FDept, FMaterial, FGroup, FChain, GetFilterSql(filter), FWL, FOrg, FSFZP, FSaleCompany);
            DBUtils.Execute(this.Context, instr1);
            //去年
            string instr2 = string.Format(instr0, tempTable02, FDept, FMaterial, FGroup, FChain, GetFilterSqlLast(filter), FWL, FOrg, FSFZP, FSaleCompany);
            DBUtils.Execute(this.Context, instr2);
            #endregion

            #region 整理汇总
            string instr3 = string.Format(@"/*dialect*/
            insert into {0}(FNAME,FID, FSTOCKNUM, FSTOCKQUOTA, FRETURNNUM, FRETURNQUOTA,FSALENUM,FSALEQUOTA,FSTOCKNUM_LAST, FSTOCKQUOTA_LAST, FRETURNNUM_LAST, FRETURNQUOTA_LAST,FSALENUM_LAST,FSALEQUOTA_LAST)
            select /*二开/KA部门*/FNAME,a.FID,nvl(FSTOCKNUM,0) FSTOCKNUM,nvl(FSTOCKQUOTA,0) FSTOCKQUOTA,
                                            nvl(FRETURNNUM,0) FRETURNNUM,nvl(FRETURNQUOTA,0) FRETURNQUOTA,
                                            nvl((FSTOCKNUM - FRETURNNUM),0) FSALENUM,nvl((FSTOCKQUOTA - FRETURNQUOTA),0) FSALEQUOTA, 
                                            nvl(FSTOCKNUM_LAST,0) FSTOCKNUM_LAST, nvl(FSTOCKQUOTA_LAST,0) FSTOCKQUOTA_LAST, 
                                            nvl(FRETURNNUM_LAST,0) FRETURNNUM_LAST, nvl(FRETURNQUOTA_LAST,0) FRETURNQUOTA_LAST,
                                            nvl((FSTOCKNUM_LAST - FRETURNNUM_LAST),0) FSALENUM_LAST,nvl((FSTOCKQUOTA_LAST - FRETURNQUOTA_LAST),0) FSALEQUOTA_LAST
            from (
                select a.FID,sum(a.FSTOCKNUM) FSTOCKNUM, sum(a.FSTOCKQUOTA) FSTOCKQUOTA, sum(a.FRETURNNUM) FRETURNNUM, 
                           sum(a.FRETURNQUOTA) FRETURNQUOTA,sum(a.FSTOCKQUOTA)-sum(a.FRETURNQUOTA) FSALEQUOTA,
                           sum(b.FSTOCKNUM) FSTOCKNUM_LAST, sum(b.FSTOCKQUOTA) FSTOCKQUOTA_LAST, sum(b.FRETURNNUM) FRETURNNUM_LAST, 
                           sum(b.FRETURNQUOTA) FRETURNQUOTA_LAST,sum(b.FSTOCKQUOTA)-sum(b.FRETURNQUOTA) FSALEQUOTA_LAST 
                     from
                          (select a.{1} FID,sum(a.FSTOCKNUM) FSTOCKNUM, sum(a.FSTOCKQUOTA) FSTOCKQUOTA, sum(a.FRETURNNUM) FRETURNNUM, 
                                   sum(a.FRETURNQUOTA) FRETURNQUOTA,sum(a.FSTOCKQUOTA)-sum(a.FRETURNQUOTA) FSALEQUOTA 
                                   from {2} a group by a.{1}) a
                           left join         
                           (select a.{1} FID,sum(a.FSTOCKNUM) FSTOCKNUM, sum(a.FSTOCKQUOTA) FSTOCKQUOTA, sum(a.FRETURNNUM) FRETURNNUM, 
                                   sum(a.FRETURNQUOTA) FRETURNQUOTA,sum(a.FSTOCKQUOTA)-sum(a.FRETURNQUOTA) FSALEQUOTA 
                                   from {3} a group by a.{1}) b
                    on a.FID = b.FID
                    group by a.FID
            ) a {4} ", tempTable03, ColumnFilter(filter), tempTable01, tempTable02, SaleProFilter(filter));
            DBUtils.Execute(this.Context, instr3);

            #endregion

            #region 最终整理
            string instrhej = string.Format(@"select sum(FSALEQUOTA) hj_now,sum(FSALEQUOTA_LAST) hj_last from {0}", tempTable03);
            DynamicObjectCollection lshj = DBUtils.ExecuteDynamicObject(this.Context, instrhej);
            string instr04 = string.Format(@"/*dialect*/
            insert into {0}(FNAME,FID, FSTOCKNUM, FSALENUM, FRETURNNUM, FSTOCKQUOTA, FSALEQUOTA, FRETURNQUOTA, FSALEPER, FRETURNPER,
                          FSTOCKNUM_LAST, FSALENUM_LAST, FRETURNNUM_LAST, FSTOCKQUOTA_LAST, FSALEQUOTA_LAST, FRETURNQUOTA_LAST, 
                          FSALEPER_LAST, FRETURNPER_LAST,FSALEQUOTAPER)
            select/*二开/KA部门*/ FNAME,FID,FSTOCKNUM, FSALENUM, FRETURNNUM, FSTOCKQUOTA, FSALEQUOTA, FRETURNQUOTA, FSALEPER, FRETURNPER,
                          FSTOCKNUM_LAST, FSALENUM_LAST, FRETURNNUM_LAST, FSTOCKQUOTA_LAST, FSALEQUOTA_LAST, FRETURNQUOTA_LAST, 
                          FSALEPER_LAST, FRETURNPER_LAST,FSALEQUOTAPE FSALEQUOTAPER
            from(
                select FNAME,FID,FSTOCKNUM,FSALENUM,FRETURNNUM,FSTOCKNUM_LAST,FSALENUM_LAST,FRETURNNUM_LAST,
                       FSTOCKQUOTA,FSALEQUOTA,FRETURNQUOTA,FSTOCKQUOTA_LAST,FSALEQUOTA_LAST,FRETURNQUOTA_LAST,
                       (case when {2} <> 0 then
                            to_char((FSALEQUOTA/{2}) * 100,'99990.00') || '%'
                            else to_char(0.00) || '%' end) FSALEPER,
                       (case when (FSTOCKQUOTA) <> 0 then
                            to_char((FRETURNQUOTA/FSTOCKQUOTA) * 100,'99990.00') || '%'
                            else to_char(0.00) || '%' end) FRETURNPER,
                       (case when {3} <> 0 then
                            to_char((FSALEQUOTA_LAST/{3}) * 100,'99990.00') || '%'
                            else to_char(0.00) || '%' end) FSALEPER_LAST,
                       (case when (FSTOCKQUOTA_LAST) <> 0 then
                            to_char((FRETURNQUOTA_LAST)/(FSTOCKQUOTA_LAST) * 100,'99990.00')||'%'
                            else to_char(0.00) || '%' end) FRETURNPER_LAST,
                       (case when FSALEQUOTA_LAST <> 0 then
                            to_char((FSALEQUOTA/FSALEQUOTA_LAST) * 100,'99990.00') || '%'
                            else to_char(0.00) || '%' end) FSALEQUOTAPE from {1}
                  )", tempTable04, tempTable03, lshj[0]["hj_now"].ToString(), lshj[0]["hj_last"].ToString());
            DBUtils.Execute(this.Context, instr04);
            #endregion

        }
        #endregion

        #region 删除临时表
        private void DropTable()
        {
            string delsql02 = string.Format("TRUNCATE TABLE {0}", tempTable002);
            DBUtils.Execute(this.Context, delsql02);
            string dropsql02 = string.Format("DROP TABLE {0}", tempTable002);
            DBUtils.Execute(this.Context, dropsql02);
            string delsql1 = string.Format("TRUNCATE TABLE {0}", tempTable01);
            DBUtils.Execute(this.Context, delsql1);
            string dropsql1 = string.Format("DROP TABLE {0}", tempTable01);
            DBUtils.Execute(this.Context, dropsql1);
            string delsql2 = string.Format("TRUNCATE TABLE {0}", tempTable02);
            DBUtils.Execute(this.Context, delsql2);
            string dropsql2 = string.Format("DROP TABLE {0}", tempTable02);
            DBUtils.Execute(this.Context, dropsql2);
            string delsql3 = string.Format("TRUNCATE TABLE {0}", tempTable03);
            DBUtils.Execute(this.Context, delsql3);
            string dropsql3 = string.Format("DROP TABLE {0}", tempTable03);
            DBUtils.Execute(this.Context, dropsql3);
            string delsql4 = string.Format("TRUNCATE TABLE {0}", tempTable04);
            DBUtils.Execute(this.Context, delsql4);
            string dropsql4 = string.Format("DROP TABLE {0}", tempTable04);
            DBUtils.Execute(this.Context, dropsql4);
        }
        #endregion

        #region 合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();

            //list.Add(new SummaryField(string.Format("FSTOCKNUM"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSALENUM"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FRETURNNUM"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSTOCKQUOTA"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSALEQUOTA"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FRETURNQUOTA"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));


            //list.Add(new SummaryField(string.Format("FSTOCKNUM_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSALENUM_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FRETURNNUM_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSTOCKQUOTA_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FSALEQUOTA_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            //list.Add(new SummaryField(string.Format("FRETURNQUOTA_LAST"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));

            return list;

        }
        #endregion

        #region  给表头赋值
        DynamicObject dep = null; DynamicObject mat = null; DynamicObject forgroup = null;
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            string dept = string.Empty; string fforgroup = string.Empty;
            if (dep != null)
            {
                dept = dep["Name"].ToString();
            }
            if (forgroup != null)
            {
                fforgroup = forgroup["FDataValue"].ToString();
            }

            string s = string.Empty;
            ReportTitles titles = new ReportTitles();
            string tableTitle = string.Empty;
            //long titleFilter = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_TL_Combo"]);
            switch (columnFilter)
            {
                case 1:
                    s = "KA部门-KA组";
                    tableTitle = string.Format(@"KA部门销售数据查询");
                    break;
                case 2:
                    s = "KA部门-产品";
                    tableTitle = string.Format(@"KA部门销售数据查询");
                    break;
                case 3:
                    s = "KA部门-产品-KA组";
                    tableTitle = string.Format(@"KA部门{0}销售数据查询", mat["Name"].ToString());
                    break;
                case 4:
                    s = "KA部门-KA组-产品";
                    tableTitle = string.Format(@"KA部门{0}销售数据查询", dept);
                    break;
                case 5:
                    s = "KA部门-KA组-连锁";
                    tableTitle = string.Format(@"KA部门{0}销售数据查询", dept);
                    break;
                case 6:
                    s = "KA部门-KA组-连锁-客户";
                    tableTitle = string.Format(@"KA部门{0}{1}销售数据查询", dept, fforgroup);
                    break;
                case 7:
                    s = "KA部门-连锁-客户";
                    tableTitle = string.Format(@"KA部门{0}销售数据查询", fforgroup);
                    break;
                default: break;
            }
            titles.AddTitle("FTITLEFILTER", tableTitle);
            titles.AddTitle("F_JF_Label", s);
            return titles;
        }
        #endregion

        #region  客户组过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FGroupFilter(IRptParams filter)
        {

            string groupID = Convert.ToString(filter.FilterParameter.CustomFilter["FDEPTTYPE"]);
            if (groupID == "")
            {
                return "";
            }
            return string.Format("and d.FDEPTTYPE='{0}'", groupID);

        }
        #endregion


        #region 销售公司过滤
        private string FDeptCompanyFilter(IRptParams filter)
        {
            DynamicObject saleCompany = (DynamicObject)filter.FilterParameter.CustomFilter["FSaleCompanyID"];
            if (saleCompany == null)
            {
                return "";
            }
            string companyNumber = Convert.ToString(saleCompany["Number"]).Substring(0, 5);
            return string.Format("and d.FNUMBER like '{0}%'", companyNumber);
        }
        #endregion
        #region  部门过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FDeptFilter(IRptParams filter)
        {
            dep = (DynamicObject)filter.FilterParameter.CustomFilter["FSALEDEPTID"];
            long deptID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEDEPTID_Id"]);
            //long deptID = Convert.ToInt64(dep["Id"]);
            if (deptID == 0)
            {
                return "";
            }
            return string.Format("and e.FSSKAZ={0}", deptID);

        }
        #endregion

        #region  单品过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FMaterialIDFilter(IRptParams filter)
        {
            mat = (DynamicObject)filter.FilterParameter.CustomFilter["FMATERIALID"];
            long materialID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FMATERIALID_Id"]);
            if (materialID == 0)
            {
                return "";
            }
            return string.Format("and a.FMATERIALID={0}", materialID);

        }
        #endregion

        #region  连锁过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FChainFilter(IRptParams filter)
        {
            forgroup = (DynamicObject)filter.FilterParameter.CustomFilter["FFORGROUPCUST"];
            string groupID = Convert.ToString(filter.FilterParameter.CustomFilter["FFORGROUPCUST_Id"]);
            if (groupID == "")
            {
                return "";
            }
            return string.Format("and e.FFORGROUPCUST='{0}'", groupID);

        }
        #endregion

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

        #region 是否赠品
        private string FSFZPFilter(IRptParams filter)
        {

            bool s = Convert.ToBoolean(filter.FilterParameter.CustomFilter["FSFZP"]);

            if (s)
            {
                return "";
            }
            return "and a.FISTASTE=' '";

        }
        #endregion

        #region 列名显示方案
        private String ColumnShowFilter(IRptParams filter)
        {
            //long columnFilter = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_TL_Combo"]);
            switch (columnFilter)
            {
                case 1: return "KA组";
                case 2: return "产品";
                case 3: return "KA组";
                case 4: return "产品";
                case 5: return "连锁";
                case 6: return "客户";
                case 7: return "客户";
                default: return "";
            }
        }
        #endregion

        #region 列名方案
        private String ColumnFilter(IRptParams filter)
        {
            switch (columnFilter)
            {
                case 1: return "FSALEDEPTID";
                case 2: return "FMATERIALID";
                case 3: return "FSALEDEPTID";
                case 4: return "FMATERIALID";
                case 5: return "fforgroupcust";
                case 6: return "FCUSTID";
                case 7: return "FCUSTID";
                default: return "";
            }
        }
        #endregion

        #region 销售占比表
        private String SaleProFilter(IRptParams filter)
        {
            //long saleProFilter = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_TL_Combo"]);
            switch (columnFilter)
            {
                case 1: return string.Format(@"
                    inner join T_BD_DEPARTMENT_L e on a.FID = e.FDEPTID");
                case 2: return string.Format(@"
                    inner join T_BD_MATERIAL_L e on a.FID = e.FMATERIALID");
                case 3: return string.Format(@"
                    inner join T_BD_DEPARTMENT_L e on a.FID = e.FDEPTID");
                case 4: return string.Format(@"             
                    inner join T_BD_MATERIAL_L e on a.FID = e.FMATERIALID");
                case 5: return string.Format(@"
                    inner join {0} b on a.fid=b.fmasterid", tempTable002);
                case 6: return string.Format(@"
                    inner join T_BD_CUSTOMER_L e on a.FID = e.FCUSTID");
                case 7: return string.Format(@"
                    inner join T_BD_CUSTOMER_L e on a.FID = e.FCUSTID");
                default: return "";
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

        /// <summary>
        /// 过滤界面起始日期、结束日期参数插入过滤语句
        /// </summary>
        private string GetFilterSql(IRptParams filter)
        {
            this.GetFilter(filter);
            string strstartday = this.dtStartDate == DateTime.MinValue ? "" : FieldFormatterUtil.GetDateFormatString(this.Context, this.dtStartDate);
            string strendday = this.dtEndDate == DateTime.MaxValue ? "" : FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate);
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(strstartday) && !string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE>=to_date('{0}','YYYY-MM-DD') and b.FDATE<=to_date('{1}','YYYY-MM-DD')", this.dtStartDate.ToString("yyyy/MM/dd "), this.dtEndDate.ToString("yyyy/MM/dd")));
                return builder.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(strstartday) && string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE>=to_date('{0}','YYYY-MM-DD')", this.dtStartDate.ToString("yyyy/MM/dd ")));
                return builder.ToString();
            }
            else if (string.IsNullOrWhiteSpace(strstartday) && !string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE<=to_date('{0}','YYYY-MM-DD')", this.dtEndDate.ToString("yyyy/MM/dd")));
                return builder.ToString();
            }
            else
            {
                return "";
            }

        }

        private string GetFilterSqlLast(IRptParams filter)
        {
            this.GetFilter(filter);
            string strstartday = this.dtStartDate == DateTime.MinValue ? "" : FieldFormatterUtil.GetDateFormatString(this.Context, this.dtStartDate.AddYears(-1));
            string strendday = this.dtEndDate == DateTime.MaxValue ? "" : FieldFormatterUtil.GetDateFormatString(this.Context, this.dtEndDate.AddYears(-1));
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(strstartday) && !string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE>=to_date('{0}','YYYY-MM-DD') and b.FDATE<=to_date('{1}','YYYY-MM-DD')", strstartday, strendday));
                return builder.ToString();
            }
            else if (!string.IsNullOrWhiteSpace(strstartday) && string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE>=to_date('{0}','YYYY-MM-DD')", strstartday));
                return builder.ToString();
            }
            else if (string.IsNullOrWhiteSpace(strstartday) && !string.IsNullOrWhiteSpace(strendday))
            {
                builder.AppendLine(string.Format(" and b.FDATE<=to_date('{0}','YYYY-MM-DD')", strendday));
                return builder.ToString();
            }
            else
            {
                return "";
            }

        }
        #endregion
    }
}
