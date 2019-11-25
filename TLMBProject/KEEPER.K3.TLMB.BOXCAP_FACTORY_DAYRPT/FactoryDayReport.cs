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

namespace KEEPER.K3.TLMB.BOXCAP_FACTORY_DAYRPT
{
    [Description("箱套-工厂日报表")]
    public class FactoryDayReport: SysReportBaseService
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
            base.ReportProperty.ReportName = new LocaleValue("箱套-工厂日报表", this.Context.UserLocale.LCID);   //报表名称
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
            var LXQIKC = header.AddChild("F_PAEZ_LXZRJZ", new LocaleValue("绿箱&昨日结转"));
            LXQIKC.ColIndex = 1;
            var LXBQFC = header.AddChild("F_PAEZ_LXBQFC", new LocaleValue("绿箱&本期发出"));
            LXBQFC.ColIndex = 2;
            var LXBQFX = header.AddChild("F_PAEZ_LXBQFX", new LocaleValue("绿箱&本期返箱"));
            LXBQFX.ColIndex = 3;
            var LXSCDX = header.AddChild("F_PAEZ_LXSCDX", new LocaleValue("绿箱&市场丢箱"));
            LXSCDX.ColIndex = 4;
            var LXBQKC = header.AddChild("F_PAEZ_LXJRJZ", new LocaleValue("绿箱&今日结转"));
            LXBQKC.ColIndex = 5;
            var BXQIKC = header.AddChild("F_PAEZ_BXQIKC", new LocaleValue("白箱&前期库存"));
            BXQIKC.ColIndex = 6;
            var BXBQFC = header.AddChild("F_PAEZ_BXZRJZ", new LocaleValue("白箱&昨日结转"));
            BXBQFC.ColIndex = 7;
            var BXBQFX = header.AddChild("F_PAEZ_BXBQFX", new LocaleValue("白箱&本期返箱"));
            BXBQFX.ColIndex = 8;
            var BXSCDX = header.AddChild("F_PAEZ_BXSCDX", new LocaleValue("白箱&市场丢箱"));
            BXSCDX.ColIndex = 9;
            var BXBQKC = header.AddChild("F_PAEZ_BXJRJZ", new LocaleValue("白箱&今日结转"));
            BXBQKC.ColIndex = 10;
            return header;
        }

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            insertData();
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as select 1 seq,
       '和平' F_PAEZ_QY,
       0 F_PAEZ_LXZRJZ,
       0 F_PAEZ_LXBQFC,
       0 F_PAEZ_LXBQFX,
       0 F_PAEZ_LXSCDX,
       0 F_PAEZ_LXJRJZ,
       0 F_PAEZ_BXQIKC,
       0 F_PAEZ_BXZRJZ,
       0 F_PAEZ_BXBQFX,
       0 F_PAEZ_BXSCDX,
       0 F_PAEZ_BXJRJZ,
       {2}
  from {1}", tableName, tempTable, KSQL_SEQ);
            //          string strsql = string.Format(@"/*dialect*/create table {0} as select *,
            //     {2}
            //from {1}", tableName, tempTable, KSQL_SEQ);
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
            string sql = string.Format(@"/*dialect*/ select/*直接调拨单*/ d1.FSALEDEPTID ,d2.FSRCMATERIALID,sum(d2.FQTY) as sum1                 /*销售部门，物料编码，sum(调拨数量) */
                                                        from T_STK_STKTRANSFERIN d1                                                       /*直接调拨单单据头*/
                                                        inner join T_STK_STKTRANSFERINENTRY d2 on d2.fid = d1.fid                         /*直接调拨单单据体*/
                                                        where d1.FSTOCKOUTORGID = {3}  and d1.FDATE < to_Date ('{0}','yyyy/MM/dd')
                                                              and （d1.FALLOCATETYPE =0 or  d1.FALLOCATETYPE=1)                           /*调拨类型 */
                                                              and  d2.FSRCSTOCKID={2}                                                     /*调出仓库*/
                                                              and (d2.FSRCMATERIALID=07040000 or d2.FSRCMATERIALID=07040004)
                                                              group by d1.FSALEDEPTID,d2.FSRCMATERIALID
", nowDateString, selectString, outStockId, nowOrgId, returnStockId);
            #endregion


        }

        private void insertData()
        {
            string strSql = string.Format(@"insert into {0} select 1 seq,
       '和平' F_PAEZ_QY,
       0 F_PAEZ_LXZRJZ,
       0 F_PAEZ_LXBQFC,
       0 F_PAEZ_LXBQFX,
       0 F_PAEZ_LXSCDX,
       0 F_PAEZ_LXJRJZ,
       0 F_PAEZ_BXQIKC,
       0 F_PAEZ_BXZRJZ,
       0 F_PAEZ_BXBQFX,
       0 F_PAEZ_BXSCDX,
       0 F_PAEZ_BXJRJZ
from dual", tempTable);
            DBUtils.Execute(base.Context, strSql);
        }

        private void CreateTempTable()
        {
            this.tempTable = addTempTable(base.Context);
            String strSql = String.Format(@"CREATE TABLE {0}
(
  seq                     VARCHAR(50),
  F_PAEZ_QY			      VARCHAR(50),
  f_paez_lxqikc           VARCHAR(50),
  F_PAEZ_LXBQFC           VARCHAR(50),
  F_PAEZ_LXBQFX		      VARCHAR(50),
  F_PAEZ_LXSCDX           VARCHAR(50),
  F_PAEZ_LXJRJZ			  VARCHAR(50),
  F_PAEZ_BXQIKC			  VARCHAR(50),
  F_PAEZ_BXZRJZ			  VARCHAR(50),
  F_PAEZ_BXBQFX			  VARCHAR(50),
  F_PAEZ_BXSCDX			  VARCHAR(50),
  F_PAEZ_BXJRJZ			  VARCHAR(50)
)", tempTable);
            DBUtils.Execute(this.Context, strSql);
        }

        private string addTempTable(Context context)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(context);
        }
    }
}
