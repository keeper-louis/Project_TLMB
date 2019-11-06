using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.DPTJ
{
    public class DPTJReport: SysReportBaseService
    {

        #region 参数设置

        //private DateTime dtEndDate;
        private string tempTable1 = string.Empty;
        private string tempTable2 = string.Empty;
        private string tempTable3 = string.Empty;
        private string tempTable1hj = string.Empty;
        private ArrayList tables = new ArrayList();
        #endregion

        #region 初始化报表参数
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            base.ReportProperty.ReportName = new LocaleValue("单品销售统计", this.Context.UserLocale.LCID);   //报表名称
        }
        #endregion

        #region 表列设置
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            header.AddChild("dname", new LocaleValue("工厂", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header.AddChild("fcaption", new LocaleValue("品类", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header.AddChild("mname", new LocaleValue("品名", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header.AddChild("fspecification", new LocaleValue("规格", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

            header.AddChild("SC", new LocaleValue("生产数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("ZT", new LocaleValue("正品数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("CK", new LocaleValue("发货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("TH", new LocaleValue("退货数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("XS", new LocaleValue("销售数量", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("fprice", new LocaleValue("单价", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);

            header.AddChild("SCCZ", new LocaleValue("生产产值", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("SCE", new LocaleValue("生产金额", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("ZPE", new LocaleValue("正品金额", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("CKE", new LocaleValue("发货金额", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("THJE", new LocaleValue("退货金额", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);
            header.AddChild("XSE", new LocaleValue("销售金额", this.Context.UserLocale.LCID), SqlStorageType.SqlDecimal);

            header.AddChild("FHL1", new LocaleValue("退货率", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);
            header.AddChild("FXSZB1", new LocaleValue("销售占比", this.Context.UserLocale.LCID), SqlStorageType.Sqlvarchar);

            return header;
        }

        #endregion

        #region   实现帐表的主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTalbe();
            this.InsertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "to_number(b.f_pl),b.fno");
            string sqlstr = string.Format(@"/*dialect*/create table {0} as  
              select dname,fcaption,mname,fspecification,SC,ZT,CK,TH,XS,fprice,SCCZ,SCE,ZTE,CKE,THJE,XSE,
               to_char(b.fhl,'99990.00')||'%' fhl1,to_char(b.fxszb,'99990.00')||'%' fxszb1,{2} 
               from {1} b", tableName, tempTable1hj, KSQL_SEQ);
            DBUtils.Execute(this.Context, sqlstr);
            string sqlstr1 = string.Format(@"/*dialect*/insert into {0}
                        select dname,null,' ',null,SC,null,null,null,null,null,null,null,null,null,null,null,' ',' ',
                        (select count(dname) from {0}) + rownum
                           from (  select dname,sum(SC) SC from {0} a where a.dname <> ' 'group by dname )", tableName);
            DBUtils.Execute(this.Context, sqlstr1);
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
            this.tempTable1 = AddTempTable(base.Context);
            this.tempTable2 = AddTempTable(base.Context);
            this.tempTable3 = AddTempTable(base.Context);
            this.tempTable1hj = AddTempTable(base.Context);
            string sqlstr1 = string.Format(@"create table {0}
                                    (FMATERIALID decimal(23,10)
                                    ,fprice decimal(23,10) )", tempTable1);
            DBUtils.Execute(this.Context, sqlstr1);
            //td.FParentID,a.fmaterialid,tm.f_Pl,tm.fno,sum(SC) SC,sum(DBZT) DBZT,
            //sum(DBGT) DBGT,sum(DBCK) DBCK,sum(CKCK) CKCK,sum(THTH) THTH,sum(THZT) THZT
            string sqlstr2 = string.Format(@"create table {0}
                                    (FParentID decimal(23,10)
                                    ,fmaterialid decimal(23,10)
                                    ,f_pl decimal(23,10)
                                    ,fno decimal(23,10) 
                                    ,SC decimal(23,10)
                                    ,DBZT decimal(23,10)
                                    ,DBGT decimal(23,10)
                                    ,DBCK decimal(23,10)
                                    ,CKCK decimal(23,10)
                                    ,THTH decimal(23,10)
                                    ,THZT decimal(23,10))", tempTable2);
            DBUtils.Execute(this.Context, sqlstr2);


        }
        #endregion

        #region 写入数据
        private void InsertData(IRptParams filter)
        {
            string rq = getDate(filter); string FOrgID = FOrgIDFilter(filter);
            //string DBRQ = "";
            string FDATE = string.Format(@"and FDATE>=to_date('{0}','YYYY/MM/DD')
                             and FDATE<add_months(to_date('{0}','YYYY/MM/DD'),1)", rq);
            //品类中文名表
            string mj = string.Format(@"/*dialect*/
            select /*二开/单品销*/ l.fcaption,le.fvalue from T_META_FORMENUMITEM_L l join T_META_FORMENUMITEM le
            on le.fenumid=l.fenumid where le.fid = '794d4c38-ee23-453c-a8f6-ecd638306f4d' order by le.fvalue");
            DynamicObjectCollection mjx = DBUtils.ExecuteDynamicObject(this.Context, mj);
            //价目表
            string instr0 = string.Format(@"/*dialect*/insert into {0}
            select /*二开/单品销*/ b.FMATERIALID,b.fprice from t_sal_pricelist a
            join t_sal_pricelistentry b on a.FID=b.FID
            where a.Fdocumentstatus='C' and FLimitCustomer='2'  and FPriceType='57eb5de168e269'
            and FSALEORGID={1} and a.fforbidstatus='A' group by b.FMATERIALID,b.fprice", tempTable1, FOrgID);
            DBUtils.ExecuteDynamicObject(this.Context, instr0);

            string DB =
                @"SELECT  f.fmaterialid,0,sum(f.fqty) DBZT,0,0,0,0,0 FROM  T_STK_STKTRANSFERIN e,T_STK_STKTRANSFERINENTRY  f
                WHERE e.fid = f.fid AND e.fsaleorgid = {0} {1} AND e.fallocatetype =3 
                AND e.fdocumentstatus = 'C' group by f.fmaterialid
                union all
                SELECT  f.fmaterialid,0,0,sum(f.fqty) DBGT,0,0,0,0 FROM  T_STK_STKTRANSFERIN e,T_STK_STKTRANSFERINENTRY  f 
                WHERE e.fid = f.fid AND e.fsaleorgid = {0} {1} AND e.fallocatetype =4
                AND e.fdocumentstatus = 'C'GROUP BY f.fmaterialid
                union all
                SELECT  f.fmaterialid,0,0,0,sum(f.fqty) DBCK,0,0,0 FROM  T_STK_STKTRANSFERIN e,T_STK_STKTRANSFERINENTRY  f
                WHERE e.fid = f.fid AND e.fsaleorgid = {0} {1} AND e.fallocatetype =1 
                AND e.fdocumentstatus = 'C'GROUP BY f.fmaterialid";
            ///////////////////////////////////////////////////////////////
            string KH =
                  @"exists (
                  select 1 from (   select c.fcustid from t_bd_customer c where c.FKHBD in ('3','4')
                                    union
                                    select c.fcustid from t_bd_customer c where c.fkhsx in ('2','3')
                                ) a where a.fcustid =";
            //////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////
            string sqlStr1 = string.Format(@"/*dialect*/insert into {2}
              select /*二开/单品销*/td.FParentID,a.fmaterialid,tm.f_Pl,tm.fno,sum(SC) SC,sum(DBZT) DBZT,
                sum(DBGT) DBGT,sum(DBCK) DBCK,sum(CKCK) CKCK,sum(THTH) THTH,sum(THZT) THZT from (
                --------------------JDSCRK
                SELECT C.FMATERIALID,sum(C.FREALQTY) SC,0 DBZT,0 DBGT,0 DBCK,0 CKCK,0 THTH,0 THZT
                FROM T_SP_INSTOCKENTRY C join T_SP_INSTOCK D on C.FID = D.FID 
                join (select k.fstockid from t_bd_stock k where k.fcklx='1') w on w.fstockid=C.FSTOCKID
                WHERE d.FStockOrgId={0} {1} 
                AND D.FDOCUMENTSTATUS = 'C' GROUP BY c.fmaterialid
                --------------------DB
                union all 
                " + DB + @"
                --------------------CK
                union all
                SELECT a.fmaterialid,0,0,0,0,SUM(a.frealqty) CKCK,0,0 FROM (
                SELECT re.fmaterialid,re.frealqty FROM T_SAL_OUTSTOCK r JOIN T_SAL_OUTSTOCKENTRY re ON re.fid=r.fid
                join (select k.fstockid from t_bd_stock k where k.fcklx='1') w on w.fstockid=re.fstockid
                WHERE " + KH + @"r.FCUSTOMERID ) and r.fsaleorgid={0} {1} 
                AND r.fdocumentstatus = 'C' ) a GROUP BY a.fmaterialid
                ---------------------TH
                union all
                SELECT b.fmaterialid,0,0,0,0,0,SUM(b.frealqty) THTH,0  FROM (
                SELECT re.fmaterialid,re.frealqty FROM t_sal_returnstock r JOIN t_sal_returnstockentry re ON re.fid=r.fid
                join (select k.fstockid from t_bd_stock k where k.fcklx='2') w on w.fstockid=re.fstockid
                WHERE " + KH + @"r.fretcustid ) and r.fsaleorgid={0} {1} 
                AND r.fdocumentstatus = 'C' ) b GROUP BY b.fmaterialid
                ---------------------ZT
                union all
                SELECT a.fmaterialid,0,0,0,0,0,0,SUM(a.frealqty) THZT FROM (
                SELECT re.fmaterialid,re.frealqty FROM t_sal_returnstock r JOIN t_sal_returnstockentry re ON re.fid=r.fid
                join (select k.fstockid from t_bd_stock k where k.fcklx='1') w on w.fstockid=re.fstockid
                WHERE " + KH + @"r.fretcustid ) and r.fsaleorgid={0} {1} AND r.FRETURNTYPE0='2'
                ) a GROUP BY a.fmaterialid ) a 
                JOIN t_BD_MaterialProduce tmp ON a.fmaterialid = tmp.fmaterialid and tmp.fmaterialid <> 0
                join t_bd_department td on td.fdeptid=tmp.Fworkshopid and td.FParentID <> 0
                JOIN T_BD_MATERIAL tm ON a.fmaterialid = tm.fmaterialid and tm.f_pl<>' ' 
                GROUP BY td.FParentID,a.fmaterialid,tm.f_Pl,tm.fno order by tm.fno", FOrgID, FDATE, tempTable2);
            DynamicObjectCollection coly = DBUtils.ExecuteDynamicObject(this.Context, sqlStr1);


            //substr(tml.fspecification,1,length(tml.fspecification)-1)
            string instr2 = string.Format(@"/*dialect*/create table {0} as
             select /*二开/单品销*/tml.fname mname,b.fmaterialid,tml.fspecification,b.FParentID,tdl.fname dname,b.f_Pl,d.fcaption,b.fno,b.sc,b.zt,b.ck,b.th,b.xs,b.fprice
            ,tmb.FGROSSWEIGHT * b.sc/1000 sccz,sce,zte,cke,thje,xse,case when cke <> 0 then (thje/cke)*100 else 0 end fhl from (
            select b.FParentID,b.fmaterialid,b.f_Pl,b.fno,b.sc,b.zt,b.ck,b.th,b.xs,nvl(c.fprice,0) fprice,
            b.sc * nvl(c.fprice,0) sce,b.zt * nvl(c.fprice,0) zte,b.ck * nvl(c.fprice,0) cke,b.th * nvl(c.fprice,0) thje,b.xs * nvl(c.fprice,0) xse from 
            (
                select a.FParentID,a.fmaterialid,a.f_Pl,a.fno,a.sc
                ,(a.dbzt + a.thzt) zt,(a.dbck + a.ckck) ck,(a.dbgt + a.thth - a.thzt) th,
                    (a.dbck + a.ckck)-(a.dbgt + a.thth - a.thzt) xs from {1} a
                ) b left join {2} c on c.FMATERIALID=b.FMATERIALID ) b join 
                ( select /*二开/单品销*/ l.fcaption,le.fvalue from T_META_FORMENUMITEM_L l join T_META_FORMENUMITEM le
                        on le.fenumid=l.fenumid where le.fid = '794d4c38-ee23-453c-a8f6-ecd638306f4d' order by le.fvalue
                ) d on b.f_Pl=d.fvalue join t_bd_material_l tml on tml.fmaterialid=b.FMATERIALID
                join t_BD_MaterialBase tmb on tml.fmaterialid=tmb.FMATERIALID
                join t_bd_department_l tdl on tdl.fdeptid=b.FParentID", tempTable3, tempTable2, tempTable1);
            DynamicObjectCollection col1 = DBUtils.ExecuteDynamicObject(this.Context, instr2);



            string sqlstrI1 = string.Format(@"/*dialect*/insert into {0} 
             select '合计',null,null,0,' ',99,null,99,sum(sc),sum(zt),sum(ck),sum(th),sum(xs),null,
             sum(sccz),sum(sce),sum(zte),sum(cke),sum(thje),sum(xse),case when sum(cke)<>0 then sum(thje)* 100/sum(cke) else 0 end from {0} a", tempTable3);

            for (int i = 0; i < mjx.Count; i++)
            {
                sqlstrI1 += string.Format(@"/*dialect*/union all
                select '小计',null,null,0,' ',{1},null,{1},sum(sc),sum(zt),sum(ck),sum(th),sum(xs),null,
                sum(sccz),sum(sce),sum(zte),sum(cke),sum(thje),sum(xse),case when sum(cke)<>0 then sum(thje)* 100/sum(cke) else 0 end from {0} a 
                where a.f_pl={2}", tempTable3, (Convert.ToInt32(mjx[i]["fvalue"])) + 0.1, Convert.ToInt32(mjx[i]["fvalue"]));
            }
            DBUtils.Execute(this.Context, sqlstrI1);

            string zs = string.Format(@"/*dialect*/select xse from {0} a where a.mname='合计'", tempTable3);
            DynamicObjectCollection zs1 = DBUtils.ExecuteDynamicObject(this.Context, zs);

            string instr3 = string.Format(@"/*dialect*/create table {0} as
             select a.*,case when {2}<>0 then xse*100/{2} else 0 end fxszb from {1} a
            ", tempTable1hj, tempTable3, zs1[0]["xse"].ToString());
            DBUtils.Execute(this.Context, instr3);

            string instr4 = string.Format(@"/*dialect*/delete {0} W where W.FXSZB is null", tempTable1hj);
            DBUtils.Execute(this.Context, instr4);
        }

        #endregion

        #region 删除临时表
        private void DropTable()
        {
            string delsql03 = string.Format("TRUNCATE TABLE {0}", tempTable1);
            DBUtils.Execute(this.Context, delsql03);
            string dropsql03 = string.Format("DROP TABLE {0}", tempTable1);
            DBUtils.Execute(this.Context, dropsql03);

            string delsql04 = string.Format("TRUNCATE TABLE {0}", tempTable2);
            DBUtils.Execute(this.Context, delsql04);
            string dropsql04 = string.Format("DROP TABLE {0}", tempTable2);
            DBUtils.Execute(this.Context, dropsql04);

            string delsql05 = string.Format("TRUNCATE TABLE {0}", tempTable3);
            DBUtils.Execute(this.Context, delsql05);
            string dropsql05 = string.Format("DROP TABLE {0}", tempTable3);
            DBUtils.Execute(this.Context, dropsql05);

            string delsql06 = string.Format("TRUNCATE TABLE {0}", tempTable1hj);
            DBUtils.Execute(this.Context, delsql06);
            string dropsql06 = string.Format("DROP TABLE {0}", tempTable1hj);
            DBUtils.Execute(this.Context, dropsql06);


        }
        #endregion

        #region 合计

        #endregion

        #region  给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            String tableTitle = string.Format(@"单品销售统计");
            ReportTitles titles = new ReportTitles();
            titles.AddTitle("FTITLEFILTER", tableTitle);
            return titles;
        }
        #endregion

        #region  过滤时间
        private string getDate(IRptParams filter)
        {
            return string.Format("{0}/{1}/1", Convert.ToString(filter.FilterParameter.CustomFilter["FYEAR"])
                                    , Convert.ToString(filter.FilterParameter.CustomFilter["FMONTH"]));
        }
        #endregion

        #region  组织过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FOrgIDFilter(IRptParams filter)
        {

            long orgID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEORGID_Id"]);
            if (orgID == 0)
            {
                return "";
            }
            return string.Format(orgID.ToString());

        }
        #endregion
    }
}
