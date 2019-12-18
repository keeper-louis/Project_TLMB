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

namespace KEEPER.K3.TLMB.BOXCAP_FACTORY_MONTHRPT
{
    [Description("箱套-工厂月报表")]
    public class FactoryMonthReport : SysReportBaseService
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
        private ArrayList tables = new ArrayList();
        #endregion
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
            base.ReportProperty.ReportName = new LocaleValue("箱套-工厂月报表", this.Context.UserLocale.LCID);   //报表名称
        }
        /// <summary>
        /// 报表动态表头构建
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        #region 构建报表列
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            var QY = header.AddChild("F_PAEZ_QY", new LocaleValue("区域"));
            QY.ColIndex = 0;
            var LXQIKC = header.AddChild("F_PAEZ_LXQIKC", new LocaleValue("绿箱&前期库存"));
            LXQIKC.ColIndex = 1;
            var LXBQFC = header.AddChild("F_PAEZ_LXBQFC", new LocaleValue("绿箱&本期发出"));
            LXBQFC.ColIndex = 2;
            var LXBQFX = header.AddChild("F_PAEZ_LXBQFX", new LocaleValue("绿箱&本期返箱"));
            LXBQFX.ColIndex = 3;
            var LXSCDX = header.AddChild("F_PAEZ_LXSCDX", new LocaleValue("绿箱&市场丢箱"));
            LXSCDX.ColIndex = 4;
            var LXBQKC = header.AddChild("F_PAEZ_LXBQKC", new LocaleValue("绿箱&本期库存"));
            LXBQKC.ColIndex = 5;
            var BXQIKC = header.AddChild("F_PAEZ_BXQIKC", new LocaleValue("白箱&前期库存"));
            BXQIKC.ColIndex = 6;
            var BXBQFC = header.AddChild("F_PAEZ_BXBQFC", new LocaleValue("白箱&本期发出"));
            BXBQFC.ColIndex = 7;
            var BXBQFX = header.AddChild("F_PAEZ_BXBQFX", new LocaleValue("白箱&本期返箱"));
            BXBQFX.ColIndex = 8;
            var BXSCDX = header.AddChild("F_PAEZ_BXSCDX", new LocaleValue("白箱&市场丢箱"));
            BXSCDX.ColIndex = 9;
            var BXBQKC = header.AddChild("F_PAEZ_BXBQKC", new LocaleValue("白箱&本期库存"));
            BXBQKC.ColIndex = 10;
            return header;
        }
        #endregion
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            InsertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as 
        select seq,f_paez_qy ,f_paez_lxqikc,f_paez_lxbqfc,f_paez_lxbqfx,f_paez_lxscdx,f_paez_lxbqkc,f_paez_bxqikc,f_paez_bxbqfc,f_paez_bxbqfx,f_paez_bxscdx,f_paez_bxbqkc,  
       {2}
  from {1}", tableName, tempTable, KSQL_SEQ);
            DBUtils.Execute(base.Context, strsql);
            DropTable();
        }

        #region 插入数据
        private void InsertData(IRptParams filter)
        {
            #region 过滤条件
            //过滤条件
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            //获取当前库存组织
            DynamicObject nowOrg = dyFilter["F_PAEZ_OrgId"] as DynamicObject;
            long nowOrgId = Convert.ToInt64(nowOrg["Id"]);
            //获取发出仓库
            DynamicObject outStock = dyFilter["F_PAEZ_OutWareHouse"] as DynamicObject;
            long outStockId = Convert.ToInt64(outStock["Id"]);
            //获取返箱仓库
            DynamicObject returnStock = dyFilter["F_PAEZ_InWareHouse"] as DynamicObject;
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

            #region 临时表插入数据
            #region 本期发出数据
            //绿箱本期发出 
            string sqlLX = string.Format(@"/*dialect*/  
                                            insert into {4}
                                                  select 0 seq,f_paez_qy ,0 f_paez_lxqikc,sum( f_paez_lxbqfc) f_paez_lxbqfc, 0 f_paez_lxbqfx,0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc from(
                                                      select/*直接调拨单*/   m4.fname f_paez_qy,sum(d2.FQTY) f_paez_lxbqfc
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID,m4.fname
                                                   union all 
                                                       select  /*销售出库单KA客户*/ m4.fname f_paez_qy,sum(s2.FREALQTY) f_paez_lxbqfc  
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and s1.FSALEDEPTID <> (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3})
                                                              group by s1.FCUSTOMERID,s2.FMATERIALID,m4.fname
                                                   union all
                                                       select  /*销售出库单KA客户*/ m4.fname f_paez_qy,sum(t2.FREALQTY) f_paez_lxbqfc
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = t1.fcustomerid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and t1.FSALEDEPTID = (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3}) 
                                                              group by t1.FCUSTOMERID,t2.FMATERIALID,m4.fname
                                        ) group by  f_paez_qy

", lastString, selectString, outStockId, nowOrgId,tempTable1);
            DBUtils.Execute(this.Context, sqlLX);
            //白箱本期发出 
            string sqlBX = string.Format(@"/*dialect*/  
                                            insert into {4}
                                                  select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc, 0 f_paez_lxbqfx,0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       sum( f_paez_bxbqfc) f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc from(
                                                      select/*直接调拨单*/   m4.fname f_paez_qy,sum(d2.FQTY) f_paez_bxbqfc
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID,m4.fname
                                                   union all 
                                                       select  /*销售出库单KA客户*/ m4.fname f_paez_qy,sum(s2.FREALQTY) f_paez_bxbqfc  
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and s1.FSALEDEPTID <> (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3})
                                                              group by s1.FCUSTOMERID,s2.FMATERIALID,m4.fname
                                                   union all
                                                       select  /*销售出库单KA客户*/ m4.fname f_paez_qy,sum(t2.FREALQTY) f_paez_bxbqfc
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = t1.fcustomerid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and t1.FSALEDEPTID = (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3}) 
                                                              group by t1.FCUSTOMERID,t2.FMATERIALID,m4.fname
                                        ) group by  f_paez_qy

", lastString, selectString, outStockId, nowOrgId, tempTable2);
            DBUtils.Execute(this.Context, sqlBX);
            #endregion
            #region 本期返箱
            //绿箱本期返箱
            string sql2LX = string.Format(@"/*dialect*/ 
                                  insert into {5} 
                                     select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc, sum(f_paez_lxbqfx) f_paez_lxbqfx,0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc  from(
                                                     select/*直接调拨单*/ m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_lxbqfx                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                   union all 
                                                      select/*销售退货*/  m4.fname f_paez_qy,sum(s2.FREALQTY) as f_paez_lxbqfx 
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fretcustid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                   union all
                                                      select/*采购入库单分公司*/  m4.fname f_paez_qy,sum(t2.FREALQTY) as f_paez_lxbqfx
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                       inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_supplier m5 on m5.fsupplierid = t1.fsupplierid
                                                        inner join t_bd_supplier_l m4 on m4.fsupplierid = t1.fsupplierid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and m5.fprimarygroup = 103581 and m5.fuseorgid = {3}
                                                              group by m4.fname)
                                                    group by F_PAEZ_QY

", lastString, selectString, returnStockId, nowOrgId, outStockId, tempTable3);
            DBUtils.Execute(this.Context, sql2LX);
            //白箱本期返箱
            string sql2BX = string.Format(@"/*dialect*/ 
                                  insert into {5} 
                                     select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc, sum(f_paez_lxbqfx) f_paez_lxbqfx,0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc  from(
                                                     select/*直接调拨单*/ m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_lxbqfx                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.fsaledeptid
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                   union all 
                                                      select/*销售退货*/  m4.fname f_paez_qy,sum(s2.FREALQTY) as f_paez_lxbqfx 
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fretcustid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                   union all
                                                      select/*采购入库单分公司*/  m4.fname f_paez_qy,sum(t2.FREALQTY) as f_paez_lxbqfx
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                       inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_supplier m5 on m5.fsupplierid = t1.fsupplierid
                                                        inner join t_bd_supplier_l m4 on m4.fsupplierid = t1.fsupplierid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and m5.fprimarygroup = 103581 and m5.fuseorgid = {3}
                                                              group by m4.fname)
                                                    group by F_PAEZ_QY

", lastString, selectString, returnStockId, nowOrgId, outStockId, tempTable4);
            DBUtils.Execute(this.Context, sql2BX);

            #endregion
            #region 市场丢箱
            //绿箱市场丢箱
            string sql3LX = string.Format(@"/*dialect*/
                                   insert into {4} 
                                     select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc,0 f_paez_lxbqfx, sum(f_paez_lxscdx) f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc  from(                                                    
                                                       select/*其他出库单(分销站丢箱)*/ m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_lxscdx                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname
                                                    Union all
                                                      select/*其他出库单(KA客户丢箱)*/  m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_lxscdx                   
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname)
                                            group by f_paez_qy
                                                  
", lastString, selectString, outStockId, nowOrgId,tempTable5);
            DBUtils.Execute(this.Context, sql3LX);
            //白箱市场丢箱
            string sql3BX = string.Format(@"/*dialect*/
                                   insert into {4} 
                                     select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc,0 f_paez_lxbqfx, 0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       sum(f_paez_bxscdx) f_paez_bxscdx,
       0 f_paez_bxbqkc  from(                                                    
                                                       select/*其他出库单(分销站丢箱)*/ m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_bxscdx                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname
                                                    Union all
                                                      select/*其他出库单(KA客户丢箱)*/  m4.fname f_paez_qy,sum(d2.FQTY) as f_paez_bxscdx                   
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname)
                                            group by f_paez_qy
                                                  
", lastString, selectString, outStockId, nowOrgId, tempTable6);
            DBUtils.Execute(this.Context, sql3BX);

            #endregion
            #region 前期库存
            //绿箱前期库存
            string sql4LX = string.Format(@"/*dialect*/ 
 insert into {5} 
                                     select 0 seq,f_paez_qy ,sum(f_paez_lxqikc) f_paez_lxqikc,0 f_paez_lxbqfc,0 f_paez_lxbqfx, 0 f_paez_lxscdx,0 f_paez_lxbqkc ,0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc  from(
                                    select fname f_paez_qy,sum(f_paez_lxqikc) f_paez_lxqikc from (
                                            select/*直接调拨单*/m4.fname,sum(d2.FQTY) as f_paez_lxqikc                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FSALEDEPTID
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                        union all 
                                                                       
                                           select/*直接调拨单*/m4.fname,-sum(d2.FQTY) as f_paez_lxqikc                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FSALEDEPTID
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={4}                                                     /*调入仓库*/
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                        union all
                                
                                           select/*其他出库单(分销站丢箱)*/m4.fname,-sum(d2.FQTY) as f_paez_lxqikc                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                      
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname                                        /*领料部门*/    
                                                              )
                                            group by fname
                                                   

                 union all 
                            select fname f_paez_qy,sum(f_paez_lxqikc) f_paez_lxqikc from (
                                               select/*销售出库单KA客户*/  m4.fname,sum(s2.FREALQTY) as f_paez_lxqikc
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid =s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                union all    

                                                      select/*销售退货（KA客户）*/  m4.fname,-sum(s2.FREALQTY) as f_paez_lxqikc
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fretcustid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname,-sum(d2.FQTY)  as f_paez_lxqikc              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname
                                                  ) group by fname
                 union all 
                            select fname f_paez_qy,sum(f_paez_lxqikc) f_paez_lxqikc from (                  
                                                      select/*销售出库单分公司*/  m4.fname,sum(t2.FREALQTY) as f_paez_lxqikc
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid =t1.fcustomerid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and t1.FSALEDEPTID = (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3}) 
                                                              group by m4.fname
                                                  union all 
                                                      select/*采购入库单分公司*/  m4.fname,sum(t2.FREALQTY) as f_paez_lxqikc
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                       inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_supplier m5 on m5.fsupplierid = t1.fsupplierid
                                                        inner join t_bd_supplier_l m4 on m4.fsupplierid = t1.fsupplierid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                    ) group by  fname
                                       )group by f_paez_qy
", lastString, selectString, outStockId, nowOrgId, returnStockId,tempTable7);
            DBUtils.Execute(this.Context, sql4LX);
            //白箱前期库存
            string sql4BX = string.Format(@"/*dialect*/ 
 insert into {5} 
                                     select 0 seq,f_paez_qy ,0 f_paez_lxqikc,0 f_paez_lxbqfc,0 f_paez_lxbqfx, 0 f_paez_lxscdx,0 f_paez_lxbqkc ,sum(f_paez_bxqikc) f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc  from(
                                    select fname f_paez_qy,sum(f_paez_bxqikc) f_paez_bxqikc from (
                                            select/*直接调拨单*/m4.fname,sum(d2.FQTY) as f_paez_bxqikc                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FSALEDEPTID
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                        union all 
                                                                       
                                           select/*直接调拨单*/m4.fname,-sum(d2.FQTY) as f_paez_bxqikc                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FSALEDEPTID
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={4}                                                     /*调入仓库*/
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                        union all
                                
                                           select/*其他出库单(分销站丢箱)*/m4.fname,-sum(d2.FQTY) as f_paez_bxqikc                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_department_l m4 on m4.fdeptid = d1.FDEPTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                      
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname                                        /*领料部门*/    
                                                              )
                                            group by fname
                                                   

                 union all 
                            select fname f_paez_qy,sum(f_paez_bxqikc) f_paez_bxqikc from (
                                               select/*销售出库单KA客户*/  m4.fname,sum(s2.FREALQTY) as f_paez_bxqikc
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid =s1.fcustomerid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                union all    

                                                      select/*销售退货（KA客户）*/  m4.fname,-sum(s2.FREALQTY) as f_paez_bxqikc
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        inner join  t_bd_material m3 on s2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = s1.fretcustid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {4}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname,-sum(d2.FQTY)  as f_paez_bxqikc              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname
                                                  ) group by fname
                 union all 
                            select fname f_paez_qy,sum(f_paez_bxqikc) f_paez_bxqikc from (                  
                                                      select/*销售出库单分公司*/  m4.fname,sum(t2.FREALQTY) as f_paez_bxqikc
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid =t1.fcustomerid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and t1.FSALEDEPTID = (select t1.fdeptid from t_bd_department t1
                                                              inner join t_bd_department_l t2 on t2.fdeptid = t1.fdeptid
                                                              where t2.fname like '工厂' and t1.fcreateorgid = {3}) 
                                                              group by m4.fname
                                                  union all 
                                                      select/*采购入库单分公司*/  m4.fname,sum(t2.FREALQTY) as f_paez_bxqikc
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                       inner join  t_bd_material m3 on t2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_supplier m5 on m5.fsupplierid = t1.fsupplierid
                                                        inner join t_bd_supplier_l m4 on m4.fsupplierid = t1.fsupplierid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              group by m4.fname
                                                    ) group by  fname
                                       )group by f_paez_qy
", lastString, selectString, outStockId, nowOrgId, returnStockId, tempTable8);
            DBUtils.Execute(this.Context, sql4BX);
            #endregion
            #region /*本期库存，前期库存(sql4LX)+本期发出(sql1LX)-本期返箱(sql2LX)-市场丢箱(sql3LX)*/
            string sql5LX = string.Format(@" /*dialect*/
                insert into {0}
                      select  
                            0 seq,
                            finall.f_paez_qy f_paez_qy,
                            0 f_paez_lxqikc,
                            0 f_paez_lxbqfc,
                            0 f_paez_lxbqfx,
                            0 f_paez_lxscdx,
                            sum(finall.f_paez_lxqikc+finall.f_paez_lxbqfc-finall.f_paez_lxbqfx-finall.f_paez_lxscdx) f_paez_lxbqkc,
                            0 f_paez_bxqikc,
                            0 f_paez_bxbqfc,
                            0 f_paez_bxbqfx,
                            0 f_paez_bxscdx,
                            sum(finall.f_paez_bxqikc+finall.f_paez_bxbqfc-finall.f_paez_bxbqfx-finall.f_paez_bxscdx) f_paez_bxbqkc
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
                            union
                            select * from {7}
                            union
                            select * from {8}
                         )finall group by finall.f_paez_qy",tempTable9, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6, tempTable7, tempTable8);
            DBUtils.Execute(this.Context, sql5LX);


            #endregion
            #region 所有数据插入基表
            //将绿箱本期发出插入到临死表中
            string strSql1 = string.Format(@"insert into {0}
                select  0 seq,finall.f_paez_qy f_paez_qy,sum(finall.f_paez_lxqikc) f_paez_lxqikc,sum(finall.f_paez_lxbqfc) f_paez_lxbqfc,sum(finall.f_paez_lxbqfx) f_paez_lxbqfx,sum(finall.f_paez_lxscdx) f_paez_lxscdx,
                            sum(finall.f_paez_lxbqkc) f_paez_lxbqkc,sum(finall.f_paez_bxqikc) f_paez_bxqikc,sum(finall.f_paez_bxbqfc) f_paez_bxbqfc,sum(finall.f_paez_bxbqfx) f_paez_bxbqfx,sum(finall.f_paez_bxscdx) f_paez_bxscdx,sum(finall.f_paez_bxbqkc) f_paez_bxbqkc
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
                    select G.* from {7} G
                    union
                    select H.* from {8} H 
                    union
                    select I.* from {9} I
                    ) finall group by finall.f_paez_qy 
", tempTable,tempTable1,tempTable2,tempTable3,tempTable4, tempTable5,tempTable6,tempTable7,tempTable8,tempTable9);
            DBUtils.Execute(this.Context, strSql1);
            #endregion


            #endregion

        }
        #endregion
        #region 创建临时表
        private void CreateTempTable()
        {
            this.tempTable1 = addTempTable(base.Context);//绿箱本期发出临时表
            this.tempTable2 = addTempTable(base.Context);
            this.tempTable3 = addTempTable(base.Context);
            this.tempTable4 = addTempTable(base.Context);
            this.tempTable5 = addTempTable(base.Context);
            this.tempTable6 = addTempTable(base.Context);
            this.tempTable7 = addTempTable(base.Context);
            this.tempTable8 = addTempTable(base.Context);
            this.tempTable9 = addTempTable(base.Context);
            this.tempTable = addTempTable(base.Context);
            //本期发出临时表
            string sqlstr1 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable1);
            DBUtils.Execute(this.Context, sqlstr1);
            //本期发出临时表
            string sqlstr2 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable2);
            DBUtils.Execute(this.Context, sqlstr2);
            //本期返箱临时表
            string sqlstr3 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable3);
            DBUtils.Execute(this.Context, sqlstr3);
            //本期返箱临时表
            string sqlstr4 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable4);
            DBUtils.Execute(this.Context, sqlstr4);
            //市场丢箱临时表
            string sqlstr5 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable5);
            DBUtils.Execute(this.Context, sqlstr5);
            //市场丢箱临时表
            string sqlstr6 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable6);
            DBUtils.Execute(this.Context, sqlstr6);
            //前期库存临时表
            string sqlstr7 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable7);
            DBUtils.Execute(this.Context, sqlstr7);
            string sqlstr8 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable8);
            DBUtils.Execute(this.Context, sqlstr8);
            //本期库存
            string sqlstr9 = string.Format(@"create table {0}
                                    (
                                      seq                     VARCHAR(50),
                                      f_paez_qy			      VARCHAR(50),
                                      f_paez_lxqikc           VARCHAR(50),
                                      f_paez_lxbqfc           VARCHAR(50),
                                      f_paez_lxbqfx		      VARCHAR(50),
                                      f_paez_lxscdx           VARCHAR(50),
                                      f_paez_lxbqkc			  VARCHAR(50),
                                      f_paez_bxqikc			  VARCHAR(50),
    f_paez_bxbqfc			  VARCHAR(50),
    f_paez_bxbqfx			  VARCHAR(50),
    f_paez_bxscdx			  VARCHAR(50),
    f_paez_bxbqkc			  VARCHAR(50)
                                    )", tempTable9);
            DBUtils.Execute(this.Context, sqlstr9);


            String strSql = String.Format(@"CREATE TABLE {0}
(
  seq                     VARCHAR(50),
  f_paez_qy			      VARCHAR(50),
  f_paez_lxqikc           VARCHAR(50),
  f_paez_lxbqfc           VARCHAR(50),
  f_paez_lxbqfx		      VARCHAR(50),
  f_paez_lxscdx           VARCHAR(50),
  f_paez_lxbqkc			  VARCHAR(50),
  f_paez_bxqikc			  VARCHAR(50),
  f_paez_bxbqfc			  VARCHAR(50),
  f_paez_bxbqfx			  VARCHAR(50),
  f_paez_bxscdx			  VARCHAR(50),
  f_paez_bxbqkc			  VARCHAR(50)
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
        }
        #endregion

        #region 给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles titles = new ReportTitles();
            string tableTitle = string.Empty;
            string tableTitle1 = string.Empty;
            //组织反写
            DynamicObject org = (DynamicObject)filter.FilterParameter.CustomFilter["F_PAEZ_OrgId"];

            if (org != null)
            {
                string depName = org["Name"].ToString();
                tableTitle = depName;
            }
            else
            {
                tableTitle = "";
            }
            //日期反写   存在bug不存在date为空的情况
            DateTime date = Convert.ToDateTime(filter.FilterParameter.CustomFilter["F_PAEZ_Date"]);
            date=date.AddDays(19);
            if (date != null)
            {
                string dateName = date.ToString();
                tableTitle1 = dateName;
            }
            else
            {
                tableTitle1 = "";
            }


            titles.AddTitle("F_PAEZ_OrgName", tableTitle);
            titles.AddTitle("F_PAEZ_Date", tableTitle1);
            return titles;
        }
        #endregion



    }
}
