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

namespace KEEPER.K3.TLMB.BOXCAP_FACTORY_MONTHRPT
{
    [Description("箱套-工厂月报表")]
    public class FactoryMonthReport: SysReportBaseService
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
            base.ReportProperty.ReportName = new LocaleValue("箱套-工厂月报表", this.Context.UserLocale.LCID);   //报表名称
        }
        /// <summary>
        /// 报表动态表头构建
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>

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

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            insertData();
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as select 1 seq,
       '和平' f_paez_qy,
       0 f_paez_lxqikc,
       0 f_paez_lxbqfc,
       0 f_paez_lxbqfx,
       0 f_paez_lxscdx,
       0 f_paez_lxbqkc,
       0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc,
       {2}
  from {1}", tableName,tempTable, KSQL_SEQ);
            DBUtils.Execute(base.Context, strsql);


            #region  ZQC
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
            int lastYear=nowYear-1;
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


           
            
            string sql = string.Format(@"/*dialect*/  select/*直接调拨单*/ d1.FSALEDEPTID ,d2.FSRCMATERIALID,sum(d2.FQTY)                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID
                                                   union all 
                                                      select/*销售出库单KA客户*/  s1.FCUSTOMERID,s2.FMATERIALID,sum(s2.FREALQTY)
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              group by s1.FCUSTOMERID,s2.FMATERIALID
                                                   union all
                                                      select/*销售出库单分公司*/  t1.FCUSTOMERID,t2.FMATERIALID,sum(t2.FREALQTY)
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and (t2.FMATERIALID=2394565 or t2.FMATERIALID=2394566)
                                                              and t1.FSALEDEPTID = 0/*工厂*/
                                                              group by t1.FCUSTOMERID,t2.FMATERIALID
                                        

", lastString, selectString, outStockId,nowOrgId);

            string sql2 = string.Format(@"/*dialect*/  select/*直接调拨单*/ d1.FSALEDEPTID ,d2.FSRCMATERIALID,sum(d2.FQTY)                  /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID
                                                   union all 
                                                      select/*销售退货*/  s1.FRETCUSTID,s2.FMATERIALID,sum(s2.FREALQTY)
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and s1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              group by s1.FRETCUSTID,s2.FMATERIALID
                                                   union all
                                                      select/*采购入库单分公司*/  t1.FSUPPLIERID,t2.FMATERIALID,sum(t2.FREALQTY)
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                        inner join t_bd_customer t3 on t3.fsupplierid=t1.fsupplyid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and t1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {4}
                                                              and (t2.FMATERIALID=2394565 or t2.FMATERIALID=2394566)
                                                              and t3.FPRIMARYGROUP=105322 /*内部105322*/
                                                              group by t1.FSUPPLIERID,t2.FMATERIALID

", lastString, selectString, returnStockId, nowOrgId,outStockId);



            string sql3 = string.Format(@"/*dialect*/  select/*其他出库单(分销站丢箱)*/ d1.FDEPTID ,d2.FMATERIALID,sum(d2.FQTY)                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and (d2.FMATERIALID=2394565 or d2.FMATERIALID=2394566)
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by d1.FDEPTID,d2.FMATERIALID
                                                    Union all
                                                      select/*其他出库单(KA客户丢箱)*/ d1.FCUSTID ,d2.FMATERIALID,sum(d2.FQTY)                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and (d2.FMATERIALID=2394565 or d2.FMATERIALID=2394566)
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by d1.FCUSTID,d2.FMATERIALID
                                                  
", lastString, selectString, outStockId, nowOrgId);

            string sql4 = string.Format(@"/*dialect*/ 
                          select sum(sum1) from 
                                (
                                            select/*直接调拨单*/ d1.FSALEDEPTID ,d2.FSRCMATERIALID,sum(d2.FQTY) as sum1                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID
                                        union all 
                                                                       
                                            select/*直接调拨单*/ d1.FSALEDEPTID ,d2.FSRCMATERIALID,-sum(d2.FQTY) as sum1                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=4)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={4}                                                     /*调入仓库*/
                                                              and (d2.FSRCMATERIALID=2394565 or d2.FSRCMATERIALID=2394566)
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID
                                        union all
                                
                                            select/*其他出库单(分销站丢箱)*/ d1.FDEPTID ,d2.FMATERIALID,-sum(d2.FQTY) as sum1                  
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                      
                                                              and  d3.FDEPT = d1.FDEPTID                                                  
                                                              and (d2.FMATERIALID=2394565 or d2.FMATERIALID=2394566)
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by d1.FDEPTID,d2.FMATERIALID                                         /*领料部门*/    
                                                              )
                                            group by FSaleDeptId,FSRCMATERIALID
                                                   

                 union all 
                            select sum(sum1) from (
                                               select/*销售出库单KA客户*/  s1.FCUSTOMERID,s2.FMATERIALID,sum(s2.FREALQTY) as sum1
                                                        from T_SAL_OUTSTOCK s1
                                                        inner join T_SAL_OUTSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {2}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              group by s1.FCUSTOMERID,s2.FMATERIALID
                                                union all    

                                                      select/*销售退货（KA客户）*/  s1.FRETCUSTID,s2.FMATERIALID,sum(s2.FREALQTY) as sum1
                                                        from T_SAL_RETURNSTOCK s1
                                                        inner join T_SAL_RETURNSTOCKENTRY s2 on s1.fid=s2.fid
                                                        where s1.FSALEORGID = {3} and  s1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and s2.FSTOCKID = {4}
                                                              and (s2.FMATERIALID=2394565 or s2.FMATERIALID=2394566)
                                                              group by s1.FRETCUSTID,s2.FMATERIALID
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ d1.FCUSTID ,d2.FMATERIALID,sum(d2.FQTY)  as sum1               
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{5}','yyyy/MM/dd') 
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and (d2.FMATERIALID=2394565 or d2.FMATERIALID=2394566)
                                                              and  d1.FDEPTID <> null
                                                              and  d1.FCUSTID = null
                                                              group by d1.FCUSTID,d2.FMATERIALID
                                                  ) group by FCUSTOMERID,FMATERIALID
                 union all 
                             select sum(sum1) from (                   
                                                      select/*销售出库单分公司*/  t1.FCUSTOMERID,t2.FMATERIALID,sum(t2.FREALQTY) as sum1
                                                        from T_SAL_OUTSTOCK t1
                                                        inner join T_SAL_OUTSTOCKENTRY t2 on t1.fid=t2.fid
                                                        where t1.FSALEORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {2}
                                                              and (t2.FMATERIALID=2394565 or t2.FMATERIALID=2394566)
                                                              and t1.FSALEDEPTID = 0/*工厂*/
                                                              group by t1.FCUSTOMERID,t2.FMATERIALID
                                                  union all 
                                                      select/*采购入库单分公司*/  t1.FSUPPLIERID,t2.FMATERIALID,sum(t2.FREALQTY) as sum1
                                                        from t_STK_InStock t1
                                                        inner join T_STK_INSTOCKENTRY t2 on t1.fid=t2.fid
                                                        /*关联供应商表，查询为内部供应商的*/
                                                        inner join t_bd_customer t3 on t3.fsupplierid=t1.fsupplyid
                                                        where t1.FSTOCKORGID = {3} and  t1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and t2.FSTOCKID = {3}
                                                              and (t2.FMATERIALID=2394565 or t2.FMATERIALID=2394566)
                                                              and t3.FPRIMARYGROUP=105322 /*内部105322*/
                                                              group by t1.FSUPPLIERID,t2.FMATERIALID
                                                    ) group by  FCUSTOMERID,FMATERIALID
", nowDateString, selectString, outStockId, nowOrgId, returnStockId,lastString);

            DBUtils.Execute(base.Context, sql);
            DBUtils.Execute(base.Context, sql2);
            DBUtils.Execute(base.Context, sql3);
            DBUtils.Execute(base.Context, sql4);
            #endregion
        }

        private void insertData()
        {
            string strSql = string.Format(@"insert into {0} select 1 seq,
       '和平' f_paez_qy,
       0 f_paez_lxqikc,
       0 f_paez_lxbqfc,
       0 f_paez_lxbqfx,
       0 f_paez_lxscdx,
       0 f_paez_lxbqkc,
       0 f_paez_bxqikc,
       0 f_paez_bxbqfc,
       0 f_paez_bxbqfx,
       0 f_paez_bxscdx,
       0 f_paez_bxbqkc
from dual",tempTable);
            DBUtils.Execute(base.Context, strSql);
        }

        private void CreateTempTable()
        {
            this.tempTable = addTempTable(base.Context);
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

        private string addTempTable(Context context)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(context);
        }

    }
}
