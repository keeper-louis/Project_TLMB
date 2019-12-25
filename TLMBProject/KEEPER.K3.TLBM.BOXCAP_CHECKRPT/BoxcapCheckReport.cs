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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLBM.BOXCAP_CHECKRPT
{
    [Description("箱套日盘点表")]
    public class BoxcapCheckReport : SysReportBaseService
    {
        #region 参数设置

        //private DateTime dtEndDate;
        private string tempTable1 = string.Empty;
        private string tempTable2 = string.Empty;
        private string tempTable3 = string.Empty;
        private string tempTable4 = string.Empty;
        private string tempTable5 = string.Empty;
        private string tempTable6 = string.Empty;
        private string tempTable7 = string.Empty;
        private string tempTable8 = string.Empty;
        private string tempTable9 = string.Empty;
        private string tempTable10 = string.Empty;
        private ArrayList tables = new ArrayList();
        long FxzId;
        long KaId;
        string tempTable = string.Empty;
        //表头取值备
        string lxAmount;
        string bxAmount;
        int all = 0;
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
        #endregion
        #region 构建报表列
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
            var LXBQKC = header.AddChild("F_PAEZ_BXGCFX", new LocaleValue("白箱&工厂发箱"));
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
        #endregion
        #region 实现主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            insertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as 
       select  seq,
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
            DBUtils.Execute(base.Context, strsql);
        }
        #endregion
        #region 插入数据
        private void insertData(IRptParams filter)
        {
            #region 过滤条件
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
            DynamicObject Fxz = dyFilter["F_PAEZ_Dept"] as DynamicObject;
            if (Fxz != null)
            {
                 FxzId = Convert.ToInt64(Fxz["Id"]);
            }
            //过滤条件 获取KA客户
            DynamicObject Ka = dyFilter["F_PAEZ_KA"] as DynamicObject;
            if (Ka != null)
            {
                 KaId = Convert.ToInt64(Ka["Id"]);
            }
            //过滤条件 获取返箱仓库
            DynamicObject returnStock = dyFilter["F_PAEZ_InStock"] as DynamicObject;
            long returnStockId = Convert.ToInt64(returnStock["Id"]);
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
                string sqlLX = string.Format(@"/*dialect*/  
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   sum(F_PAEZ_LXGCFX) F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*直接调拨单*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_LXGCFX               /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE
                          )group by F_PAEZ_Date", lastString, selectString, outStockId, nowOrgId, FxzId,tempTable1);
                DBUtils.Execute(base.Context, sqlLX);
                string sqlBX = string.Format(@"/*dialect*/  
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   sum(F_PAEZ_BXGCFX) F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*直接调拨单*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_BXGCFX               /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE
) group by f_paez_date", lastString, selectString, outStockId, nowOrgId, FxzId, tempTable2);
                DBUtils.Execute(base.Context, sqlBX);
                #endregion
                #region 返厂箱数
                string sql2LX = string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   sum(F_PAEZ_LXFCXS) F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*直接调拨单*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as  F_PAEZ_LXFCXS                /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调入仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE )group by f_paez_date", lastString, selectString, outStockId, nowOrgId, FxzId,tempTable3);
                DBUtils.Execute(base.Context, sql2LX);
                string sql2BX = string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_DATE,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   sum(F_PAEZ_BXFCXS) F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*直接调拨单*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as  F_PAEZ_BXFCXS                /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调入仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID = {4}
                                                              group by d1.FDATE )group by f_paez_date", lastString, selectString, outStockId, nowOrgId, FxzId, tempTable4);
                DBUtils.Execute(base.Context, sql2BX);
                #endregion
                #region 市场丢箱
                string sql3LX= string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   sum(F_PAEZ_LXDXS) F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*其他出库单(分销站丢箱)*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_LXDXS                   
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {4}
                                                              and  d1.FCUSTID = null
                                                              group by d1.FDATE)group by F_PAEZ_Date ", lastString, selectString, returnStockId, nowOrgId, FxzId,tempTable5);
                DBUtils.Execute(base.Context, sql3LX);
                string sql3BX = string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   sum(F_PAEZ_LXDXS) F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*其他出库单(分销站丢箱)*/ d1.FDATE as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_LXDXS                   
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {4}
                                                              and  d1.FCUSTID = null
                                                              group by d1.FDATE)group by F_PAEZ_Date ", lastString, selectString, returnStockId, nowOrgId, FxzId, tempTable6);
                DBUtils.Execute(base.Context, sql3BX);
                #endregion
                #region 前期库存，前期发出-前期返箱-前期市场丢货
                //绿箱前期库存
                string sql4LX = string.Format(@"/*dialect*/ 
insert into {5}
                          select sum(FQIKC) FQIKC from 
                                (
                                        select/*直接调拨单*/ sum(d2.FQTY) as FQIKC                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={1}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and d1.FSALEDEPTID = {3}
                                                              group by d2.FSRCMATERIALID
                                        union all 
                                                                       
                                            select/*直接调拨单*/ -sum(d2.FQTY) as FQIKC                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {2}  and d1.FDATE > to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={4}                                                     /*调入仓库*/
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and d1.FSALEDEPTID = {3}
                                                              group by d2.FSRCMATERIALID
                                        union all
                                
                                            select/*其他出库单(分销站丢箱)*/ -sum(d2.FQTY) as FQIKC                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                      
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and  d1.FDEPTID ={3}
                                                              and  d1.FCUSTID = null
                                                              group by d2.FMATERIALID                                     /*领料部门*/    
                                                              
                                            )", lastString, outStockId, nowOrgId, FxzId, returnStockId,tempTable7);
                DBUtils.Execute(base.Context, sql4LX);
                //白箱前期库存
                string sql4BX = string.Format(@"/*dialect*/ 
insert into {5}
                          select sum(FQIKC) FQIKC from 
                                (
                                        select/*直接调拨单*/ sum(d2.FQTY) as FQIKC                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={1}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and d1.FSALEDEPTID = {3}
                                                              group by d2.FSRCMATERIALID
                                        union all 
                                                                       
                                            select/*直接调拨单*/ -sum(d2.FQTY) as FQIKC                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={4}                                                     /*调入仓库*/
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and d1.FSALEDEPTID = {3}
                                                              group by d2.FSRCMATERIALID
                                        union all
                                
                                            select/*其他出库单(分销站丢箱)*/ -sum(d2.FQTY) as FQIKC                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                      
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and  d1.FDEPTID ={3}
                                                              and  d1.FCUSTID = null
                                                              group by d2.FMATERIALID                                     /*领料部门*/    
                                                              
                                            )", lastString, outStockId, nowOrgId, FxzId, returnStockId, tempTable8);
                DBUtils.Execute(base.Context, sql4BX);


                #endregion
                #region 差额
                //绿箱差额
                string sql5LX = string.Format(@" /*dialect*/
                insert into {0}
                      select  
       0  seq,
       finall.F_PAEZ_Date F_PAEZ_Date,
       0 F_PAEZ_LXGCFX,
       0 F_PAEZ_LXFCXS,
       0 F_PAEZ_LXDXS,
       sum(finall.F_PAEZ_LXGCFX-finall.F_PAEZ_LXFCXS-finall.F_PAEZ_LXDXS) F_PAEZ_LXCE,
       0 F_PAEZ_BXGCFX,
       0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       sum(finall.F_PAEZ_BXGCFX-finall.F_PAEZ_BXFCXS-finall.F_PAEZ_BXDXS) F_PAEZ_BXCE,
       sum(finall.F_PAEZ_LXGCFX+finall.F_PAEZ_BXGCFX) F_PAEZ_GCFXZS,
       sum(finall.F_PAEZ_LXFCXS+finall.F_PAEZ_BXFCXS) F_PAEZ_FCXZS,
       sum(finall.F_PAEZ_LXDXS+finall.F_PAEZ_BXDXS) F_PAEZ_DXZS,   
       sum((finall.F_PAEZ_LXGCFX-finall.F_PAEZ_LXFCXS-finall.F_PAEZ_LXDXS)+(finall.F_PAEZ_BXGCFX-finall.F_PAEZ_BXFCXS-finall.F_PAEZ_BXDXS)) F_PAEZ_ZCE
                      from  (
                            select * from {1}
                            union
                            select * from {2}
                            union
                            select * from {3}
                            union
                            select * from {4}
                            union
                            select * from {5}
                            union
                            select * from {6}
                         )finall group by finall.F_PAEZ_DATE", tempTable9, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6, tempTable7, tempTable8);
                DBUtils.Execute(this.Context, sql5LX);
                #endregion
            }
            //工厂-KA客户
            else if (kaOrFxz.Equals("2"))
            {

                #region 工厂发箱
                string sqlLX = string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   sum(F_PAEZ_LXGCFX) F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from( 
                                                        select/*销售出库单KA客户*/  s1.fdate F_PAEZ_Date,sum(s2.FREALQTY) F_PAEZ_LXGCFX
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and s1.FCUSTOMERID={4}
                                                              group by s1.FDATE
						)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable1);
                DBUtils.Execute(base.Context, sqlLX);
                string sqlBX= string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   sum(F_PAEZ_BXGCFX) F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from( 
                                                        select/*销售出库单KA客户*/  s1.fdate F_PAEZ_Date,sum(s2.FREALQTY) F_PAEZ_BXGCFX
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and s1.FCUSTOMERID={4}
                                                              group by s1.FDATE
						)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable2);
                DBUtils.Execute(base.Context, sqlBX);
                #endregion
                #region 返厂箱数
                string sql2LX= string.Format(@"/*dialect*/ 
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   sum(F_PAEZ_LXFCXS) F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*销售退货*/  s1.FDATE as F_PAEZ_Date,sum(s2.FREALQTY) as F_PAEZ_LXFCXS 
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and   s1.FRETCUSTID = {4}  /*退货客户=KA客户*/
                                                              group by s1.FDATE
						)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable3);
                DBUtils.Execute(base.Context, sql2LX);
                string sql2BX = string.Format(@"/*dialect*/ 
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   sum(F_PAEZ_BXFCXS) F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(
                                                        select/*销售退货*/  s1.FDATE as F_PAEZ_Date,sum(s2.FREALQTY) as F_PAEZ_BXFCXS 
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and   s1.FRETCUSTID = {4}  /*退货客户=KA客户*/
                                                              group by s1.FDATE
						)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable4);
                DBUtils.Execute(base.Context, sql2BX);
                #endregion
                #region 市场丢箱
                string sql3LX= string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   sum(F_PAEZ_LXDXS) F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(															  
                                                        select/*其他出库单(分销站丢箱)*/ d1.FDATE  as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_LXDXS                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
														inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = null
                                                              and  d1.FCUSTID = {4}
                                                              group by d1.FDATE)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable6);
                DBUtils.Execute(base.Context, sql3LX);
                string sql3BX = string.Format(@"/*dialect*/
insert into {5}
select 0 seq,
	   F_PAEZ_Date,
	   0 F_PAEZ_LXGCFX,
	   0 F_PAEZ_LXFCXS,
	   0 F_PAEZ_LXDXS,
	   0 F_PAEZ_LXCE,
	   0 F_PAEZ_BXGCFX,
	   0 F_PAEZ_BXFCXS,
       sum(F_PAEZ_BXDXS) F_PAEZ_BXDXS,
       0 F_PAEZ_BXCE,
       0 F_PAEZ_GCFXZS,
       0 F_PAEZ_FCXZS， 
	   0 F_PAEZ_DXZS，
	   0 F_PAEZ_ZCE  from(															  
                                                        select/*其他出库单(分销站丢箱)*/ d1.FDATE  as F_PAEZ_Date,sum(d2.FQTY) as F_PAEZ_BXDXS                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
														inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = null
                                                              and  d1.FCUSTID = {4}
                                                              group by d1.FDATE)group by F_PAEZ_Date", lastString, selectString, returnStockId, nowOrgId, KaId, tempTable6);
                DBUtils.Execute(base.Context, sql3BX);
                #endregion
                #region 前期库存，前期发出-前期返箱-前期市场丢货
                //前期库存
                string sql4LX= string.Format(@"/*dialect*/ 
insert into {5}
                          select sum(FQIKC) FQIKC from (
                                               select/*销售出库单KA客户*/ sum(s2.FREALQTY) as FQIKC
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {2} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {1}
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and s1.FCUSTOMERID = {3}
                                                              group by s2.FMATERIALID
                                                union all    

                                                      select/*销售退货（KA客户）*/ -sum(s2.FREALQTY) as FQIKC
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {2} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and s1.FRETCUSTID = {3}
                                                              group by s2.FMATERIALID
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ -sum(d2.FQTY)  as FQIKC               
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
														inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        where d1.FSTOCKORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {1}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={2})
                                                              and  d1.FDEPTID = null
                                                              and  d1.FCUSTID = {3}
                                                              group by d2.FMATERIALID
                                                  ) ", lastString, outStockId, nowOrgId, KaId, returnStockId, tempTable7);
                DBUtils.Execute(base.Context, sql4LX);
                string sql4BX = string.Format(@"/*dialect*/ 
insert into {5}
                          select sum(FQIKC) FQIKC from (
                                               select/*销售出库单KA客户*/ sum(s2.FREALQTY) as FQIKC
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {2} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {1}
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and s1.FCUSTOMERID = {3}
                                                              group by s2.FMATERIALID
                                                union all    

                                                      select/*销售退货（KA客户）*/ -sum(s2.FREALQTY) as FQIKC
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
														inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        where s1.FSALEORGID = {2} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and s1.FRETCUSTID = {3}
                                                              group by s2.FMATERIALID
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ -sum(d2.FQTY)  as FQIKC               
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
														inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        where d1.FSTOCKORGID = {2}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {1}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={2})
                                                              and  d1.FDEPTID = null
                                                              and  d1.FCUSTID = {3}
                                                              group by d2.FMATERIALID
                                                  ) ", lastString, outStockId, nowOrgId, KaId, returnStockId, tempTable8);
                DBUtils.Execute(base.Context, sql4BX);
                #endregion
                #region 差额
                //绿箱差额
                string sql5LX = string.Format(@" /*dialect*/
                insert into {0}
                      select  
       0  seq,
       finall.F_PAEZ_Date F_PAEZ_Date,
       0 F_PAEZ_LXGCFX,
       0 F_PAEZ_LXFCXS,
       0 F_PAEZ_LXDXS,
       sum(finall.F_PAEZ_LXGCFX-finall.F_PAEZ_LXFCXS-finall.F_PAEZ_LXDXS) F_PAEZ_LXCE,
       0 F_PAEZ_BXGCFX,
       0 F_PAEZ_BXFCXS,
       0 F_PAEZ_BXDXS,
       sum(finall.F_PAEZ_BXGCFX-finall.F_PAEZ_BXFCXS-finall.F_PAEZ_BXDXS) F_PAEZ_BXCE,
       sum(finall.F_PAEZ_LXGCFX+finall.F_PAEZ_BXGCFX) F_PAEZ_GCFXZS,
       sum(finall.F_PAEZ_LXFCXS+finall.F_PAEZ_BXFCXS) F_PAEZ_FCXZS,
       sum(finall.F_PAEZ_LXDXS+finall.F_PAEZ_BXDXS) F_PAEZ_DXZS,   
       sum((finall.F_PAEZ_LXGCFX-finall.F_PAEZ_LXFCXS-finall.F_PAEZ_LXDXS)+(finall.F_PAEZ_BXGCFX-finall.F_PAEZ_BXFCXS-finall.F_PAEZ_BXDXS)) F_PAEZ_ZCE
                      from  (
                            select * from {1}
                            union
                            select * from {2}
                            union
                            select * from {3}
                            union
                            select * from {4}
                            union
                            select * from {5}
                            union
                            select * from {6}
                         )finall group by finall.F_PAEZ_DATE", tempTable9, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6, tempTable7, tempTable8);
                DBUtils.Execute(this.Context, sql5LX);
                #endregion
                #region 绿箱本期结存
                string sql6LX = string.Format(@"/*dialect*/ insert into {0}  select sum(F_PAEZ_LXCE) F_PAEZ_LXCE,sum(F_PAEZ_BXCE) F_PAEZ_BXCE from {1}", tempTable10, tempTable9);
                DBUtils.Execute(this.Context, sql6LX);
                #endregion
            }
            #region 所有数据插入基表
            string strSql = string.Format(@"insert into {0} 
      select 0 seq,
       finall.F_PAEZ_Date F_PAEZ_Date,
       sum(finall.F_PAEZ_LXGCFX) F_PAEZ_LXGCFX,
       sum(finall.F_PAEZ_LXFCXS) F_PAEZ_LXFCXS,
       sum(finall.F_PAEZ_LXDXS) F_PAEZ_LXDXS,
       sum(finall.F_PAEZ_LXCE) F_PAEZ_LXCE,
       sum(finall.F_PAEZ_BXGCFX) F_PAEZ_BXGCFX,
       sum(finall.F_PAEZ_BXFCXS) F_PAEZ_BXFCXS,
       sum(finall.F_PAEZ_BXDXS) F_PAEZ_BXDXS,
       sum(finall.F_PAEZ_BXCE) F_PAEZ_BXCE,
       sum(finall.F_PAEZ_GCFXZS) F_PAEZ_GCFXZS,
       sum(finall.F_PAEZ_FCXZS) F_PAEZ_FCXZS,
       sum(finall.F_PAEZ_DXZS) F_PAEZ_DXZS,
       sum(finall.F_PAEZ_ZCE) F_PAEZ_ZCE
                    from (
                    select A.* from {1} A
                    union
                    select B.* from {2} B
                    union
                    select C.* from {3} C
                    union
                    select D.* from {4} D
                    union
                    select E.* from {5} E 
                    union
                    select F.* from {6} F
                    union
                    select F.* from {7} F
                    ) finall group by finall.F_PAEZ_Date 
", tempTable, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6,tempTable9);
            DBUtils.Execute(base.Context, strSql);
            #endregion
            #endregion
        }
        #endregion
        #region 创建临时表
        private void CreateTempTable()
        {
            this.tempTable1 = addTempTable(base.Context);
            this.tempTable2 = addTempTable(base.Context);
            this.tempTable3 = addTempTable(base.Context);
            this.tempTable4 = addTempTable(base.Context);
            this.tempTable5 = addTempTable(base.Context);
            this.tempTable6 = addTempTable(base.Context);
            this.tempTable7 = addTempTable(base.Context);
            this.tempTable8 = addTempTable(base.Context);
            this.tempTable9 = addTempTable(base.Context);
            this.tempTable10 = addTempTable(base.Context);
            this.tempTable = addTempTable(base.Context);
            string sqlstr1 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable1);
            DBUtils.Execute(this.Context, sqlstr1);
            string sqlstr2 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable2);
            DBUtils.Execute(this.Context, sqlstr2);
            string sqlstr3 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable3);
            DBUtils.Execute(this.Context, sqlstr3);
            string sqlstr4 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable4);
            DBUtils.Execute(this.Context, sqlstr4);
            string sqlstr5 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable5);
            DBUtils.Execute(this.Context, sqlstr5);
            string sqlstr6 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable6);
            DBUtils.Execute(this.Context, sqlstr6);
            string sqlstr7 = string.Format(@"/*dialect*/create table {0}
(

  FQIKC                     VARCHAR(50)

)", tempTable7);
            DBUtils.Execute(this.Context, sqlstr7);
            string sqlstr8 = string.Format(@"/*dialect*/create table {0}
(

  FQIKC2                     VARCHAR(50)

)", tempTable8);
            DBUtils.Execute(this.Context, sqlstr8);
            string sqlstr9 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
)", tempTable9);
            DBUtils.Execute(this.Context, sqlstr9);
            string sqlstr10 = string.Format(@"/*dialect*/create table {0}
(
  F_PAEZ_LXCE               VARCHAR(50),
  F_PAEZ_BXCE               VARCHAR(50)
)", tempTable10);
            DBUtils.Execute(this.Context, sqlstr10);


            String strSql = String.Format(@"/*dialect*/CREATE TABLE {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_Date               date,
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
        #endregion
        #region 添加临时表
        private string addTempTable(Context context)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(context);
        }
        #endregion
        #region 删除临时表
        private void DropTable()
        {
            string delsql02 = string.Format("TRUNCATE TABLE {0}", tempTable1);
            DBUtils.Execute(this.Context, delsql02);
            string dropsql02 = string.Format("DROP TABLE {0}", tempTable1);
            DBUtils.Execute(this.Context, dropsql02);
            string delsql1 = string.Format("TRUNCATE TABLE {0}", tempTable2);
            DBUtils.Execute(this.Context, delsql1);
            string dropsql1 = string.Format("DROP TABLE {0}", tempTable2);
            DBUtils.Execute(this.Context, dropsql1);
            string delsql2 = string.Format("TRUNCATE TABLE {0}", tempTable3);
            DBUtils.Execute(this.Context, delsql2);
            string dropsql2 = string.Format("DROP TABLE {0}", tempTable3);
            DBUtils.Execute(this.Context, dropsql2);
            string delsql3 = string.Format("TRUNCATE TABLE {0}", tempTable4);
            DBUtils.Execute(this.Context, delsql3);
            string dropsql3 = string.Format("DROP TABLE {0}", tempTable4);
            DBUtils.Execute(this.Context, dropsql3);

            string delsql5 = string.Format("TRUNCATE TABLE {0}", tempTable5);
            DBUtils.Execute(this.Context, delsql5);
            string dropsql05 = string.Format("DROP TABLE {0}", tempTable5);
            DBUtils.Execute(this.Context, dropsql05);
            string delsql6 = string.Format("TRUNCATE TABLE {0}", tempTable6);
            DBUtils.Execute(this.Context, delsql6);
            string dropsql06 = string.Format("DROP TABLE {0}", tempTable6);
            DBUtils.Execute(this.Context, dropsql06);
            string delsql7 = string.Format("TRUNCATE TABLE {0}", tempTable7);
            DBUtils.Execute(this.Context, delsql7);
            string dropsql07 = string.Format("DROP TABLE {0}", tempTable7);
            DBUtils.Execute(this.Context, dropsql07);
            string delsql8 = string.Format("TRUNCATE TABLE {0}", tempTable8);
            DBUtils.Execute(this.Context, delsql8);
            string dropsql08 = string.Format("DROP TABLE {0}", tempTable8);
            DBUtils.Execute(this.Context, dropsql08);
            string delsql9 = string.Format("TRUNCATE TABLE {0}", tempTable9);
            DBUtils.Execute(this.Context, delsql9);
            string dropsql09 = string.Format("DROP TABLE {0}", tempTable9);
            DBUtils.Execute(this.Context, dropsql09);
            string delsql10 = string.Format("TRUNCATE TABLE {0}", tempTable10);
            DBUtils.Execute(this.Context, delsql10);
            string dropsql10 = string.Format("DROP TABLE {0}", tempTable10);
            DBUtils.Execute(this.Context, dropsql10);
        }
        #endregion
        #region 给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            var result = base.GetReportTitles(filter);
            DynamicObject customerFilter = filter.FilterParameter.CustomFilter;
            if (customerFilter != null)
            {
                if(result == null)
                {
                    result = new ReportTitles();
                }
            }
            string tableTitle = string.Empty;
            string tableTitle1 = string.Empty;
            string tableTitle2 = string.Empty;
            string tableTitle3 = string.Empty;
            //绿箱前期库存量
            string sqlLXQCKC = string.Format(@"/*dialect*/ select * from {0}",tempTable7);
            DynamicObjectCollection Lx = DBUtils.ExecuteDynamicObject(this.Context, sqlLXQCKC);
            if (Lx.Count>0)
            {
                lxAmount = Convert.ToString(Lx[0]["FQIKC"]);
            }
            
            if (lxAmount != "")
            {
                tableTitle = lxAmount;
            }
            else 
            {
                tableTitle = "0";
            }
            result.AddTitle("F_PAEZ_Qikcl", tableTitle);
            //白箱前期库存量
            string sqlBXQCKC = string.Format(@"/*dialect*/ select * from {0}", tempTable8);
            DynamicObjectCollection Bx = DBUtils.ExecuteDynamicObject(this.Context, sqlBXQCKC);
            if (Bx.Count > 0)
            {
                bxAmount = Convert.ToString(Bx[0]["FQIKC2"]);
            }
            if (lxAmount != "")
            {
                tableTitle1 = bxAmount;
            }
            else
            {
                tableTitle1 = "0";
            }
            result.AddTitle("F_PAEZ_Qikcl2", tableTitle1);
            //绿箱本期库存量
            string sqlBQKC = string.Format(@"/*dialect*/ select F_PAEZ_LXCE from {0}", tempTable10);
            DynamicObjectCollection LxBq = DBUtils.ExecuteDynamicObject(this.Context, sqlBQKC);
            if (LxBq.Count>0)
            {
                int ce=Convert.ToInt32(LxBq[0]["F_PAEZ_LXCE"]);
                all=Convert.ToInt32(tableTitle)+ce;
            }
            if (all > 0)
            {
                tableTitle2 = Convert.ToString(all);
            }
            else
            {
                tableTitle2 = "0";
            }
            result.AddTitle("F_PAEZ_Bqjcs", tableTitle2);
            //白箱本期库存量
            string sqlBQKC2 = string.Format(@"/*dialect*/ select F_PAEZ_BXCE from {0}", tempTable10);
            DynamicObjectCollection BxBq = DBUtils.ExecuteDynamicObject(this.Context, sqlBQKC2);
            if (BxBq.Count > 0)
            {
                int ce = Convert.ToInt32(BxBq[0]["F_PAEZ_BXCE"]);
                all = Convert.ToInt32(tableTitle1) + ce;
            }
            if (all > 0)
            {
                tableTitle2 = Convert.ToString(all);
            }
            else
            {
                tableTitle2 = "0";
            }
            result.AddTitle("F_PAEZ_Bqjcs2", tableTitle3);
            return result;
        }
        #endregion
    }
}