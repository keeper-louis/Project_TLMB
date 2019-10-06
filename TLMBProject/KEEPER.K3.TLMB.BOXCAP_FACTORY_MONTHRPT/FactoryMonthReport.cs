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
