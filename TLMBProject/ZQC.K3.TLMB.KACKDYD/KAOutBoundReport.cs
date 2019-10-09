using Kingdee.BOS;
using Kingdee.BOS.App;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Report;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZQC.K3.TLMB.KACKDYD
{
    [Description("KA出库单报表")]
    public class KAOutBoundReport : SysReportBaseService
   
    {
        #region 参数设置
        long columnFilter = 0;//列过滤条件
        private string tempTable002 = string.Empty;
        #endregion


        #region 初始化报表参数
        public override void Initialize()
        {
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FIDENTITYID"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
            base.ReportProperty.ReportName = new LocaleValue("KA出库数据查询", this.Context.UserLocale.LCID);   //报表名称
        }
        #endregion

        #region 表列设置

        public override ReportHeader GetReportHeaders(IRptParams filter)
        {
            ReportHeader header= new ReportHeader();
            ListHeader header1 = header.AddChild();
            ListHeader header2 = header.AddChild();
            ListHeader header3 = header.AddChild();
            header1.Caption = new LocaleValue("客户名称", this.Context.UserLocale.LCID);
            header2.Caption = new LocaleValue("单据编码", this.Context.UserLocale.LCID);
            header3.Caption = new LocaleValue("提货日期", this.Context.UserLocale.LCID);
            // 编号
            var seq = header1.AddChild("FSEQ", new LocaleValue("序号"));
            seq.ColIndex = 0;
            var materialId = header1.AddChild("FMATERIALID", new LocaleValue("物料编码"));
            materialId.ColIndex = 1;

            var name = header1.AddChild("FNAME", new LocaleValue("品名"));
            name.ColIndex = 2;

            var specifiCation = header2.AddChild("FSPECIFICATION", new LocaleValue("规格"), SqlStorageType.SqlDecimal);
            specifiCation.ColIndex = 3;
            //待定  装箱数
            var packingQty = header2.AddChild("FPACKQTY ", new LocaleValue("装箱数"), SqlStorageType.SqlInt);
            packingQty.ColIndex = 4;

            var price = header2.AddChild("FTAXPRICE", new LocaleValue("含税单价"), SqlStorageType.SqlMoney);
            price.ColIndex = 5;

            var relQty = header2.AddChild("FREALQTY", new LocaleValue("实发数量"), SqlStorageType.SqlInt);
            relQty.ColIndex = 6;
            //待定  箱数
            var packQty = header2.AddChild("FPACKERQTY", new LocaleValue("箱数"), SqlStorageType.SqlInt);
            packQty.ColIndex = 7;

            var produceDate = header3.AddChild("FPRODUCEDATE", new LocaleValue("生产日期"), SqlStorageType.SqlDatetime);
            produceDate.ColIndex = 8;
            //保质期
            var expiryPeriod = header3.AddChild("FEXPPERIOD ", new LocaleValue("保质期"), SqlStorageType.SqlnText);
            expiryPeriod.ColIndex = 9;


            //结算金额（含税金额* 实发数量）
            var amount = header3.AddChild("FALLAMOUNT", new LocaleValue("结算金额"), SqlStorageType.SqlMoney);
            amount.ColIndex = 10;

            return header;
        }
        #endregion

        #region   实现帐表的主方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            columnFilter = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_PAEZ_OrgId"]);//列过滤条件   组织id
            this.CreateTempTalbe();
            this.InsertData(filter);
            base.KSQL_SEQ = string.Format(base.KSQL_SEQ, "F_PAEZ_OrgId");
            string s1 = string.Empty;



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

        //进行临时表的创建
        private void CreateTempTalbe()
        {
            this.tempTable002 = AddTempTable(base.Context);
            string sqlstr02 = string.Format(@"create table {0}
                                            (FSEQ varchar(1000)
                                            ,FMATERIALID varchar(1000)
                                            ,FNAME varchar(1000)
                                            ,FSPECIFICATION decimal(23,10)
                                            ,FPACKQTY decimal(23,10)
                                            ,FTAXPRICE decimal(23,10)
                                            ,FREALQTY decimal(23,10)
                                            ,FPACKERQTY decimal(23,10)
                                            ,FPRODUCEDATE decimal(23,10)
                                            ,FEXPPERIOD decimal(23,10)
                                            ,FALLAMOUNT decimal(23,10)
                                             )", tempTable002);
            DBUtils.Execute(this.Context, sqlstr02);
        }
        #endregion

        #region 写入数据
        private void InsertData(IRptParams filter)
        {
            #region 将查询数据插入
            string instr02 = string.Format(@"/*dialect*/
                                        insert into {0}(FSEQ，FMATERIALID，FNAME，FSPECIFICATION，FPACKQTY，FTAXAMOUNT，FREALQTY，FPACKERQTY，FPRODUCEDATE，FEXPPERIOD，FALLAMOUNT)
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
                                             outstockentryf.FALLAMOUNT，
                                        from t_Sal_Outstock outstock 
                                        inner  join T_SAL_OUTSTOCKENTRY salentry on outstock.fid = salentry.fid
                                        inner join T_BD_MATERIAL   material on material.fmaterialid= salentry.fentryid
                                        inner join T_BD_MATERIAL_L materiall on material.fmaterialid = materiall.fmaterialid
										inner join T_SAL_OUTSTOCKFIN salfin on salfin.fid=outstock.fid
										inner join T_SAL_OUTSTOCKENTRY_F outstockentryf on outstockentryf.fentryid=salfin.fentryid
                                        inner join T_BD_UNIT unit on unit.FUNITID= salentry.funitid
                                        inner join t_BD_MaterialStock materialstock on materialstock.fmaterialid=salentry.fmaterialid)", tempTable002);
            DBUtils.Execute(this.Context, instr02);
            #endregion
        }
        #endregion




        #region 删除临时表
        private void DropTable()
        {
            string delsql02 = string.Format("TRUNCATE TABLE {0}", tempTable002);
            DBUtils.Execute(this.Context, delsql02);
            string dropsql02 = string.Format("DROP TABLE {0}", tempTable002);
            DBUtils.Execute(this.Context, dropsql02);

        }

        #endregion

        #region  给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles titles = new ReportTitles();
            String tableTitle = string.Empty;



            titles.AddTitle("F_PAEZ_OrgId", tableTitle);
            titles.AddTitle("F_PAEZ_UserId", tableTitle);
            titles.AddTitle("F_PAEZ_Date", tableTitle);
            return titles;
        }
        #endregion


        #region 部门过滤
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string FOrgFilter(IRptParams filter)
        {
            long orgID = Convert.ToInt64(filter.FilterParameter.CustomFilter["FSALEORGID"]);
            if (orgID == 0)
            {
                return "";
            }
            return string.Format("and e.FSSKAZ={0}", orgID);
        }
        #endregion

    }
}
