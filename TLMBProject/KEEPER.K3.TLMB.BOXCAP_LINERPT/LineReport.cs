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

namespace KEEPER.K3.TLMB.BOXCAP_LINERPT
{
    [Description("线路箱套统计表")]
    public class LineReport: SysReportBaseService
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
        long FxzId;
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
            base.ReportProperty.ReportName = new LocaleValue("线路箱套统计表", this.Context.UserLocale.LCID);   //报表名称
        }
        /// <summary>
        /// 报表动态表头构建
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        #endregion
        #region 报表列构建
        /// <summary>
        /// 报表动态表头构建
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>

        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header = new ReportHeader();
            var QY = header.AddChild("F_PAEZ_XL", new LocaleValue("线路"));
            QY.ColIndex = 0;
            var LXQIKC = header.AddChild("F_PAEZ_LXQQKC", new LocaleValue("绿箱&前期库存"));
            LXQIKC.ColIndex = 1;
            var LXBQFC = header.AddChild("F_PAEZ_LXFXZFX", new LocaleValue("绿箱&分销站发箱"));
            LXBQFC.ColIndex = 2;
            var LXBQFX = header.AddChild("F_PAEZ_LXFZXS", new LocaleValue("绿箱&返站箱数"));
            LXBQFX.ColIndex = 3;
            var LXSCDX = header.AddChild("F_PAEZ_LXDXS", new LocaleValue("绿箱&丢箱数"));
            LXSCDX.ColIndex = 4;
            var LXBQKC = header.AddChild("F_PAEZ_LXBQKC", new LocaleValue("绿箱&本期库存数"));
            LXBQKC.ColIndex = 5;
            var BXQIKC = header.AddChild("F_PAEZ_BXQQKC", new LocaleValue("白箱&前期库存"));
            BXQIKC.ColIndex = 6;
            var BXBQFC = header.AddChild("F_PAEZ_BXFXZFX", new LocaleValue("白箱&分销站发箱"));
            BXBQFC.ColIndex = 7;
            var BXBQFX = header.AddChild("F_PAEZ_BXFZXS", new LocaleValue("白箱&返站箱数"));
            BXBQFX.ColIndex = 8;
            var BXSCDX = header.AddChild("F_PAEZ_BXDXS", new LocaleValue("白箱&丢箱数"));
            BXSCDX.ColIndex = 9;
            var BXBQKC = header.AddChild("F_PAEZ_BXBQKC", new LocaleValue("白箱&本期库存数"));
            BXBQKC.ColIndex = 10;
            return header;
        }
        #endregion
        #region 实现账表主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            InsertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as 
select seq,
       F_PAEZ_XL,
       F_PAEZ_LXQQKC,
       F_PAEZ_LXFXZFX,
       F_PAEZ_LXFZXS,
       F_PAEZ_LXDXS,
       F_PAEZ_LXBQKC,
       F_PAEZ_BXQQKC,
       F_PAEZ_BXFXZFX,
       F_PAEZ_BXFZXS,
       F_PAEZ_BXDXS,
       F_PAEZ_BXBQKC,
       {2}
  from {1}", tableName, tempTable, KSQL_SEQ);
            DBUtils.Execute(base.Context, strsql);
            DropTable();
        }
        #endregion

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
            //过滤条件 获取分销站
            DynamicObject Fxz = dyFilter["F_PAEZ_Dept"] as DynamicObject;
            if (Fxz != null)
            {
                FxzId = Convert.ToInt64(Fxz["Id"]);
            }

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
            #region 分销站发箱
            string sqlLX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       sum(F_PAEZ_LXFXZFX) F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_LXFXZFX
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=2)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname)
                        group by F_PAEZ_XL
", lastString, selectString, outStockId, nowOrgId, tempTable1, FxzId);
            DBUtils.Execute(base.Context, sqlLX);
            string sqlBX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       sum(F_PAEZ_BXFXZFX) F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_BXFXZFX
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=2)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname)
                        group by F_PAEZ_XL
", lastString, selectString, outStockId, nowOrgId, tempTable2, FxzId);
            DBUtils.Execute(base.Context, sqlBX);
            #endregion
            #region 返站箱数
            string sql2LX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       sum(F_PAEZ_LXFZXS) F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                  select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_LXFZXS
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and  d1.FALLOCATETYPE =0                    /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调入仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname)
                        group by F_PAEZ_XL
", lastString, selectString, outStockId, nowOrgId, tempTable3, FxzId);
            DBUtils.Execute(base.Context, sql2LX);
            string sql2BX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       sum(F_PAEZ_LXFZXS) F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                  select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_LXFZXS
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')  /*上月日期与选择日期*/
                                                              and  d1.FALLOCATETYPE =0                    /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调入仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname)
                        group by F_PAEZ_XL
", lastString, selectString, outStockId, nowOrgId, tempTable4, FxzId);
            DBUtils.Execute(base.Context, sql2BX);
            #endregion
            #region 丢箱数
            string sql3LX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       sum(F_PAEZ_LXDXS) F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname as F_PAEZ_XL,sum(d2.FQTY)  as F_PAEZ_LXDXS              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join T_TL_LINE_L m4 on  m4.fid  = d1.f_paez_line
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                                              
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {5}
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname    )
                        group by F_PAEZ_XL
", lastString, selectString, null, nowOrgId, tempTable5, FxzId);
            DBUtils.Execute(base.Context, sql3LX);
            string sql3BX = string.Format(@"/*dialect*/insert into {4}
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       sum(F_PAEZ_BXDXS) F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname as F_PAEZ_XL,sum(d2.FQTY)  as F_PAEZ_BXDXS              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join T_TL_LINE_L m4 on  m4.fid  = d1.f_paez_line
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE >= to_Date ('{0}','yyyy/MM/dd') and d1.FDATE <= to_Date('{1}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                                              
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {5}
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname    )
                        group by F_PAEZ_XL
", lastString, selectString, null, nowOrgId, tempTable6, FxzId);
            DBUtils.Execute(base.Context, sql3BX);
            #endregion
            #region 前期库存，前期发出-前期返箱-前期市场丢货
            //绿箱前期库存
            string sql4LX = string.Format(@"/*dialect*/ 
 insert into {4} 
       select  
       0 seq,
       F_PAEZ_XL,
       sum(F_PAEZ_LXQQKC) F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                            select F_PAEZ_XL,sum(F_PAEZ_LXQQKC) F_PAEZ_LXQQKC from (
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_LXQQKC
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=2)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname
                                                union all    
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_LXQQKC
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040000'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname   
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname as F_PAEZ_XL,sum(d2.FQTY)  F_PAEZ_LXQQKC              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040000' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {5}
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname 
                                                  ) group by F_PAEZ_XL
                
                                       )group by F_PAEZ_XL
", lastString, null, outStockId, nowOrgId, tempTable7, FxzId);
            DBUtils.Execute(this.Context, sql4LX);
            //白箱前期库存
            string sql4BX = string.Format(@"/*dialect*/ 
 insert into {4} 
       select  
       0 seq,
       F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       sum(F_PAEZ_BXQQKC) F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC  from (
                            select F_PAEZ_XL,sum(F_PAEZ_BXQQKC) F_PAEZ_BXQQKC from (
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_BXQQKC
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=2)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname
                                                union all    
                                                      select/*直接调拨单*/   m4.fname F_PAEZ_XL,sum(d2.FQTY) F_PAEZ_BXQQKC
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
														inner join T_TL_LINE_L m4 on  m4.fid  = d1.fline
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd') 
                                                              and （d1.FALLOCATETYPE =0)                           /*调拨类型 */
                                                              and  d2.FDESTSTOCKID={2}                                                     /*调出仓库*/
                                                               and ( m3.fnumber='07040004'  and m3.fuseorgid={3})
                                                              and d1.FSALEDEPTID ={5}
                                                              and d1.FLINE<>null
                                                              group by m4.fname   
                                                union all
                                                       select/*其他出库单(KA客户丢箱)*/ m4.fname as F_PAEZ_XL,sum(d2.FQTY)  F_PAEZ_BXQQKC              
                                                        from T_STK_MISDELIVERY d1                                                       
                                                        inner join T_STK_MISDELIVERYENTRY d2 on d2.fid = d1.fid                         
                                                        inner join t_bd_stock d3 on d3.fstockid=d2.fstockid
                                                        inner join  t_bd_material m3 on d2.fmaterialid = m3.fmaterialid
                                                        inner join t_bd_customer_l m4 on m4.fcustid = d1.FCUSTID
                                                        where d1.FSTOCKORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FBILLTYPEID ='5d9748dd76f550' or  d1.FBILLTYPEID='5d9748bd76f4ca')                           
                                                              and  d2.FSTOCKID = {2}                                                                         /*发货仓库=发出仓库*/                                                 
                                                              and ( m3.fnumber='07040004' and m3.fuseorgid={3})
                                                              and  d1.FDEPTID = {5}
                                                              and  d1.FCUSTID = null
                                                              group by m4.fname 
                                                  ) group by F_PAEZ_XL
                
                                       )group by F_PAEZ_XL
", lastString, null, outStockId, nowOrgId, tempTable8, FxzId);
            DBUtils.Execute(this.Context, sql4BX);
            #endregion
            #region 本期库存，前期库存+线路发箱-返线箱数-丢箱数
            string sql5LX = string.Format(@" /*dialect*/
                insert into {0}
       select  
       0 seq,
       finall.F_PAEZ_XL F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       sum(finall.F_PAEZ_LXQQKC+finall.F_PAEZ_LXFXZFX-finall.F_PAEZ_LXFZXS-finall.F_PAEZ_LXDXS) F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       sum(finall.F_PAEZ_BXQQKC+finall.F_PAEZ_BXFXZFX-finall.F_PAEZ_BXFZXS-finall.F_PAEZ_BXDXS) F_PAEZ_BXBQKC
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
                         )finall group by finall.F_PAEZ_XL", tempTable9, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6, tempTable7, tempTable8);
            DBUtils.Execute(this.Context, sql5LX);
            #endregion
            #region 所有数据插入到基表中
            string strSql = string.Format(@"insert into {0} 
        select 0 seq,
       F_PAEZ_XL,
       sum(finall.F_PAEZ_LXQQKC) F_PAEZ_LXQQKC,
       sum(finall.F_PAEZ_LXFXZFX) F_PAEZ_LXFXZFX,
       sum(finall.F_PAEZ_LXFZXS) F_PAEZ_LXFZXS,
       sum(finall.F_PAEZ_LXDXS) F_PAEZ_LXDXS,
       sum(finall.F_PAEZ_LXBQKC) F_PAEZ_LXBQKC,
       sum(finall.F_PAEZ_BXQQKC) F_PAEZ_BXQQKC,
       sum(finall.F_PAEZ_BXFXZFX) F_PAEZ_BXFXZFX,
       sum(finall.F_PAEZ_BXFZXS) F_PAEZ_BXFZXS,
       sum(finall.F_PAEZ_BXDXS) F_PAEZ_BXDXS,
       sum(finall.F_PAEZ_BXBQKC) F_PAEZ_BXBQKC
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

                    ) finall group by finall.F_PAEZ_XL 
", tempTable, tempTable1, tempTable2, tempTable3, tempTable4, tempTable5, tempTable6, tempTable7, tempTable8, tempTable9);
            DBUtils.Execute(this.Context, strSql);
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
            string sqlstr1 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable1);
            DBUtils.Execute(this.Context, sqlstr1);
            string sqlstr2 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable2);
            DBUtils.Execute(this.Context, sqlstr2);
            string sqlstr3 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable3);
            DBUtils.Execute(this.Context, sqlstr3);
            string sqlstr4 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable4);
            DBUtils.Execute(this.Context, sqlstr4);
            string sqlstr5 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable5);
            DBUtils.Execute(this.Context, sqlstr5);
            string sqlstr6 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable6);
            DBUtils.Execute(this.Context, sqlstr6);
            string sqlstr7 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable7);
            DBUtils.Execute(this.Context, sqlstr7);
            string sqlstr8 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable8);
            DBUtils.Execute(this.Context, sqlstr8);
            string sqlstr9 = string.Format(@"/*dialect*/create table {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
)", tempTable9);
            DBUtils.Execute(this.Context, sqlstr9);
            String strSql = String.Format(@"CREATE TABLE {0}
(
  seq                       VARCHAR(50),
  F_PAEZ_XL               VARCHAR(50),
  F_PAEZ_LXQQKC            VARCHAR(50), 
  F_PAEZ_LXFXZFX           VARCHAR(50),
  F_PAEZ_LXFZXS            VARCHAR(50),
  F_PAEZ_LXDXS             VARCHAR(50),
  F_PAEZ_LXBQKC            VARCHAR(50),
  F_PAEZ_BXQQKC            VARCHAR(50),
  F_PAEZ_BXFXZFX           VARCHAR(50),
  F_PAEZ_BXFZXS            VARCHAR(50),
  F_PAEZ_BXDXS             VARCHAR(50),
  F_PAEZ_BXBQKC            VARCHAR(50)
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
    }
}
