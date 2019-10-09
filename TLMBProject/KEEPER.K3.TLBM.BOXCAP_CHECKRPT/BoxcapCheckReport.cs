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
