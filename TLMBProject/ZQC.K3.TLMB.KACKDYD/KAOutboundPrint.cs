using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQC.K3.TLMB.KACKD
{
    [Description("KA出库单报表")]
    public class KAOutboundPrint : SysReportBaseService
    {
        #region  报表属性初始化
        public override void Initialize()
        {

            base.Initialize();
            // 简单账表类型：普通
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            // 报表名称
            this.ReportProperty.ReportName = new LocaleValue("KA出库单报表", base.Context.UserLocale.LCID);
            // 通过插件创建临时表
            this.IsCreateTempTableByPlugin = true;
            // 
            this.ReportProperty.IsUIDesignerColumns = false;
            // 
            this.ReportProperty.IsGroupSummary = true;
            // 
            this.ReportProperty.SimpleAllCols = false;
            // 单据主键：两行FID相同，则为同一单的两条分录，单据编号可以不重复显示
            this.ReportProperty.PrimaryKeyFieldName = "FID";
            // 
            this.ReportProperty.IsDefaultOnlyDspSumAndDetailData = true;

            // 报表主键字段名：默认为FIDENTITYID，可以修改
            //this.ReportProperty.IdentityFieldName = "FIDENTITYID";
            //
        }
        #endregion
        #region 获取表名
        public override string GetTableName()
        {
            var result = base.GetTableName();
            return result;
        }
        #endregion

        #region 实现报表结构
        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            
            ReportHeader header = new ReportHeader();
            // 编号
            var seq = header.AddChild("FSEQ", new LocaleValue("序号"));
            seq.ColIndex = 0;
            var materialId = header.AddChild("FMATERIALID", new LocaleValue("物料编码"));
            materialId.ColIndex = 1;
           
            var name = header.AddChild("FNAME", new LocaleValue("品名"));
            name.ColIndex = 2;

            var specifiCation = header.AddChild("FSPECIFICATION", new LocaleValue("规格"), SqlStorageType.SqlDecimal);
            specifiCation.ColIndex = 3;
            //待定  装箱数
            var packingQty = header.AddChild("FPACKQTY ", new LocaleValue("装箱数"), SqlStorageType.SqlInt);
            packingQty.ColIndex = 4;

            var price= header.AddChild("FTAXPRICE", new LocaleValue("含税单价"), SqlStorageType.SqlMoney);
            price.ColIndex = 5;

            var relQty = header.AddChild("FREALQTY", new LocaleValue("实发数量"), SqlStorageType.SqlInt);
            relQty.ColIndex = 6;
            //待定  箱数
            var packQty=header.AddChild("FPACKERQTY", new LocaleValue("箱数"), SqlStorageType.SqlInt);
            packQty.ColIndex = 7;

            var produceDate = header.AddChild("FPRODUCEDATE", new LocaleValue("生产日期"), SqlStorageType.SqlDatetime);
            produceDate.ColIndex = 8;
            //保质期
            var expiryPeriod = header.AddChild("FEXPPERIOD ", new LocaleValue("保质期"), SqlStorageType.SqlnText);
            expiryPeriod.ColIndex = 9;

            
            //结算金额（含税金额* 实发数量）
            var amount = header.AddChild("FBILLALLAMOUNT", new LocaleValue("结算金额"), SqlStorageType.SqlMoney);
            amount.ColIndex = 10;

            return header;
        }
        #endregion

        #region 实现账表的方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            //拼接过滤条件
           // filter.FilterParameter
            long deptID = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_PAEZ_OrgId"]);
            

            // 默认排序字段：需要从filter中取用户设置的排序字段
            string seqFld = string.Format(base.KSQL_SEQ, " F_PAEZ_OrgId ");

            // 取数SQL
            // 序号，物料编码，品名，规格，装箱数，含税单价，实发数量，箱数，生产日期 ，保质期 ，结算金额
            string sql = string.Format(@"/*dialect*/
                                        select outstock.FBILLNO,
                                             salentry.FSEQ,
                                             salentry.FMATERIALID,
                                             materiall.FNAME,
                                             materiall.FSPECIFICATION,
                                             salentry.FPACKQTY,
                                             /** FTAXPRICE含税单价*/
                                            outstockentryf.FTAXAMOUNT,
                                             salentry.FREALQTY,
                                             salentry.FPACKERQTY,
                                             salentry.FPRODUCEDATE,
                                             materialstock.FEXPPERIOD,
                                             salfin.FBILLALLAMOUNT,
                                            {0}
                                        into {1}     
                                        from t_Sal_Outstock outstock 
                                        inner  join T_SAL_OUTSTOCKENTRY salentry on outstock.fid = salentry.fid
                                        inner join T_BD_MATERIAL   material on material.fmaterialid= salentry.fentryid
                                        inner join T_BD_MATERIAL_L materiall on material.fmaterialid = materiall.fmaterialid
                                        inner join T_BD_UNIT unit on unit.FUNITID= salentry.funitid
                                        inner join t_BD_MaterialStock materialstock on materialstock.fmaterialid=salentry.fmaterialid
                                        inner join T_SAL_OUTSTOCKFIN salfin on salfin.fid=outstock.fid;
                                        ",
                         seqFld,
                         tableName);
            DBUtils.ExecuteDynamicObject(this.Context, sql);
        }
        #endregion

        #region 由索引情况判定是否重写创建账表临时表索引sql
        protected override string GetIdentityFieldIndexSQL(string tableName)
        {
            string result = base.GetIdentityFieldIndexSQL(tableName);
            return result;
        }
        #endregion

        #region 执行sql
        protected override void ExecuteBatch(List<string> listSql)
        {
            base.ExecuteBatch(listSql);
        }
        #endregion

        #region 给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {

            var result = base.GetReportTitles(filter);
            DynamicObject dyFilter = filter.FilterParameter.CustomFilter;
            if (dyFilter != null)
            {
                if (result == null)
                {
                    result = new ReportTitles();
                }
                result.AddTitle("F_TL_Outbound", Convert.ToString(dyFilter["F_TL_Outbound"]));
            }
            return result;
        }
        #endregion


        protected override string AnalyzeDspCloumn(IRptParams filter, string tablename)
        {
            string result = base.AnalyzeDspCloumn(filter, tablename);
            return result;
        }
        protected override void AfterCreateTempTable(string tablename)
        {
            base.AfterCreateTempTable(tablename);
        }

        #region 报表合计列 暂时不需要
        /// <summary>
        /// 设置报表合计列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        //public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        //{
        //    var result = base.GetSummaryColumnInfo(filter);
        //    result.Add(new SummaryField("FQty", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
        //    result.Add(new SummaryField("FALLAMOUNT", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
        //    return result;
        //}
        #endregion

        #region 获取合并列sql 暂时不需要
        protected override string GetSummaryColumsSQL(List<SummaryField> summaryFields)
        {
            var result = base.GetSummaryColumsSQL(summaryFields);
            return result;
        }
        #endregion

        protected override System.Data.DataTable GetListData(string sSQL)
        {
            var result = base.GetListData(sSQL);
            return result;
        }
        protected override System.Data.DataTable GetReportData(IRptParams filter)
        {
            var result = base.GetReportData(filter);
            return result;
        }
        protected override System.Data.DataTable GetReportData(string tablename, IRptParams filter)
        {
            var result = base.GetReportData(tablename, filter);
            return result;
        }
        public override int GetRowsCount(IRptParams filter)
        {
            var result = base.GetRowsCount(filter);
            return result;
        }
        protected override string BuilderFromWhereSQL(IRptParams filter)
        {
            string result = base.BuilderFromWhereSQL(filter);
            return result;
        }
        protected override string BuilderSelectFieldSQL(IRptParams filter)
        {
            string result = base.BuilderSelectFieldSQL(filter);
            return result;
        }
        protected override string BuilderTempTableOrderBySQL(IRptParams filter)
        {
            string result = base.BuilderTempTableOrderBySQL(filter);
            return result;
        }
        public override void CloseReport()
        {
            base.CloseReport();
        }
        protected override string CreateGroupSummaryData(IRptParams filter, string tablename)
        {
            string result = base.CreateGroupSummaryData(filter, tablename);
            return result;
        }
        protected override void CreateTempTable(string sSQL)
        {
            base.CreateTempTable(sSQL);
        }
        public override void DropTempTable()
        {
            base.DropTempTable();
        }
        public override System.Data.DataTable GetList(IRptParams filter)
        {
            var result = base.GetList(filter);
            return result;
        }
        public override List<long> GetOrgIdList(IRptParams filter)
        {
            var result = base.GetOrgIdList(filter);
            return result;
        }
        public override List<Kingdee.BOS.Core.Metadata.TreeNode> GetTreeNodes(IRptParams filter)
        {
            var result = base.GetTreeNodes(filter);
            return result;
        }
    }
}
