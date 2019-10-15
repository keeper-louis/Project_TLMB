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

            base.ReportProperty.ReportName = new LocaleValue("KA出库单查询", this.Context.UserLocale.LCID);
            base.Initialize();
            base.ReportProperty.IdentityFieldName = "FSeq"; //顺序字段名
            base.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;  //报表类型常量
            base.ReportProperty.IsGroupSummary = true;    //报表是否支持分组汇总
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

            var price = header.AddChild("FTAXAMOUNT", new LocaleValue("含税单价"), SqlStorageType.SqlMoney);
            price.ColIndex = 5;

            var relQty = header.AddChild("FREALQTY", new LocaleValue("实发数量"), SqlStorageType.SqlInt);
            relQty.ColIndex = 6;
            //待定  箱数
            var packQty = header.AddChild("FPACKERQTY", new LocaleValue("箱数"), SqlStorageType.SqlInt);
            packQty.ColIndex = 7;

            var produceDate = header.AddChild("FPRODUCEDATE", new LocaleValue("生产日期"), SqlStorageType.SqlDatetime);
            produceDate.ColIndex = 8;
            //保质期
            var expiryPeriod = header.AddChild("FEXPPERIOD ", new LocaleValue("保质期"), SqlStorageType.SqlnText);
            expiryPeriod.ColIndex = 9;


            //结算金额（含税金额* 实发数量）
            var amount = header.AddChild("FALLAMOUNT", new LocaleValue("结算金额"), SqlStorageType.SqlMoney);
            amount.ColIndex = 10;

            return header;
        }
        #endregion

        #region 实现账表的方法
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            // 默认排序字段：需要从filter中取用户设置的排序字段
            string seqFld = string.Format(base.KSQL_SEQ, "salentry.FSEQ");
            // 序号，物料编码，品名，规格，装箱数，含税单价，实发数量，箱数，生产日期 ，保质期 ，结算金额
            string sql = string.Format(@"/*dialect*/
                                   create table {1} as    
                                          select 
                                              {0},
                                             salentry.FMATERIALID,
                                             materiall.FNAME,
                                             materiall.FSPECIFICATION,
                                             salentry.FPACKQTY,
                                             outstockentryf.ftaxamount,
                                             salentry.FREALQTY,
                                             salentry.FPACKERQTY,
                                             salentry.FPRODUCEDATE,
                                             materialstock.FEXPPERIOD,
                                             outstockentryf.FALLAMOUNT,
                                             outstock.FSALEORGID,  
                                             outstock.FCUSTOMERID,
                                             to_Date(outstock.FDATE,'yyyy-MM-dd') as FDATE
                                             
                                
                                            
                                        from t_Sal_Outstock outstock 
                                        inner  join T_SAL_OUTSTOCKENTRY salentry on outstock.fid = salentry.fid 
                                        inner join T_BD_MATERIAL_L materiall on materiall.fmaterialid = salentry.fid                                        
                                        inner join T_SAL_OUTSTOCKENTRY_F outstockentryf on outstockentryf.fentryid=salentry.fentryid
                                        inner join t_BD_MaterialStock materialstock on materialstock.fmaterialid=salentry.fmaterialid
                                        inner join T_ORG_Organizations org on outstock.FSALEORGID = org.forgid
                                        inner join  T_BD_CUSTOMER  customer on customer.FCUSTID=outstock.FCUSTOMERID
                                        where 1=1
                                        ",
                         seqFld,
                         tableName);
            //组织过滤
            DynamicObject dep = (DynamicObject)filter.FilterParameter.CustomFilter["F_PAEZ_OrgId"];
            if (dep != null)
        { 
                string orgId = dep["Id"].ToString();
                sql = sql + "and org.forgid =  '" + orgId + "'";
            }
            else
            {
                sql = sql + "";
            }
            //客户过滤FCUSTOMERID
            DynamicObject user = (DynamicObject)filter.FilterParameter.CustomFilter["F_PAEZ_UserId"];

            if (user != null)
            {
                string userName = user["Id"].ToString();
                sql=sql+"and outstock.FCUSTOMERID =  '"+userName+"'";
            }
            else
            {
                sql = sql + "";
            }
            //日期过滤
           string date = Convert.ToDateTime(filter.FilterParameter.CustomFilter["F_PAEZ_Date"]).ToString("yyyy-MM-dd");
            DateTime dt = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.CurrentCulture);
            if (date != null)
            {
               sql = sql + "and FDATE = to_date('" + date+"','yyyy-MM-dd')";
            }else
            {
                sql = sql + "";
            }

            DBUtils.ExecuteDynamicObject(this.Context, sql);
        }
        #endregion


        

        #region 给表头赋值
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            ReportTitles titles = new ReportTitles();
            string tableTitle = string.Empty;
            string tableTitle1 = string.Empty;
            string tableTitle2 = string.Empty;
            //组织反写
            DynamicObject dep = (DynamicObject)filter.FilterParameter.CustomFilter["F_PAEZ_OrgId"];
            
            if (dep != null)
            {
                string depName = dep["Name"].ToString();
                tableTitle = depName;
            }else
            {
                tableTitle = "";
            }
            //日期反写   存在bug不存在date为空的情况
            DateTime date = Convert.ToDateTime(filter.FilterParameter.CustomFilter["F_PAEZ_Date"]);
            
            if (date != null)
            {
                string dateName = date.ToString();
                tableTitle1 = dateName;
            }
            else
            {
                tableTitle1 = "";
            }
            //用户反写
            DynamicObject user = (DynamicObject)filter.FilterParameter.CustomFilter["F_PAEZ_UserId"];
           
            if(user!=null)
            {
                string userName = user["Name"].ToString();
                tableTitle2 = userName;
            }
            else
            {
                tableTitle2 = "";
            }

            titles.AddTitle("F_PAEZ_OrgId", tableTitle);
            titles.AddTitle("F_PAEZ_Date", tableTitle1);
            titles.AddTitle("F_PAEZ_UserId", tableTitle2);

          

            return titles;
        }
        #endregion

        #region 合计
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            List<SummaryField> list = new List<SummaryField>();
            list.Add(new SummaryField(string.Format("FREALQTY"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            list.Add(new SummaryField(string.Format("FPACKERQTY"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            list.Add(new SummaryField(string.Format("FALLAMOUNT"), Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            return list;
        }
        #endregion


    }
}












//string user = filter.FilterParameter.CustomFilter["F_PAEZ_UserId"].ToString();
//String tableTitle2 = string.Empty;
//tableTitle = string.Format((@"{0}"), userName);
//tableTitle2 = string.Format((@"{0}"), user);
//titles.AddTitle("F_PAEZ_UserId", tableTitle);
//titles.AddTitle("F_PAEZ_UserId", user);
//tableTitle1 = string.Format((@"{0}"), date);



//private string FOrgFilter(IRptParams filter)
//{

//    long deptID = Convert.ToInt64(filter.FilterParameter.CustomFilter["F_PAEZ_OrgId_Id"]);
//    if (deptID == 0)
//    {
//        return "";
//    }
//    return string.Format("= {0}", deptID);
//}

