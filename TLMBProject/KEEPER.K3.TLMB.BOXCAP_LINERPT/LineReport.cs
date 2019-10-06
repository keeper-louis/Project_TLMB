using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using System;
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

        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            this.CreateTempTable();//创建临时表，用于数据整理
            insertData();
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "seq");
            string strsql = string.Format(@"/*dialect*/create table {0} as select 1 seq,
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
            //          string strsql = string.Format(@"/*dialect*/create table {0} as select *,
            //     {2}
            //from {1}", tableName, tempTable, KSQL_SEQ);
            DBUtils.Execute(base.Context, strsql);

        }

        private void insertData()
        {
            string strSql = string.Format(@"insert into {0} select 1 seq,
       '线路1' F_PAEZ_XL,
       0 F_PAEZ_LXQQKC,
       0 F_PAEZ_LXFXZFX,
       0 F_PAEZ_LXFZXS,
       0 F_PAEZ_LXDXS,
       0 F_PAEZ_LXBQKC,
       0 F_PAEZ_BXQQKC,
       0 F_PAEZ_BXFXZFX,
       0 F_PAEZ_BXFZXS,
       0 F_PAEZ_BXDXS,
       0 F_PAEZ_BXBQKC
from dual", tempTable);
            DBUtils.Execute(base.Context, strSql);
        }

        private void CreateTempTable()
        {
            this.tempTable = addTempTable(base.Context);
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

        private string addTempTable(Context context)
        {
            return ServiceHelper.GetService<IDBService>().CreateTemporaryTableName(context);
        }
    }
}
