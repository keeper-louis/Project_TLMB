using Kingdee.BOS.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using KEEPER.K3.TLMB.Contracts;
using Newtonsoft.Json.Linq;
using Kingdee.BOS.App.Data;
using System.Data;
using Kingdee.BOS.ServiceHelper;

namespace KEEPER.K3.TLMB.BIGDATA.PValueScheduleServicePlugIn
{
    [Description("获取第二天配送预测值")]
    public class PValueScheduleService : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            string strSql = string.Format(@"/*dialect*/truncate table T_MID_REFERENCE");
            DBUtils.Execute(ctx, strSql);
            ClientFactory clientHelper = new ClientFactory();
            JObject jo = new JObject();
            string Mod_time = DateTime.Now.AddDays(1).ToShortDateString();
            jo.Add("modelId", "xgb_toly_01");
            jo.Add("mod_time", Mod_time);
            string result = clientHelper.execute<string>("202325", "ebafe6b2aaf3db31978f71a7cd981de2", jo);
            JObject rss = JObject.Parse(result);
            JArray arry = rss["data"] as JArray;

            // 定义一个DataTable：存储待更新的数据
            DataTable dt = new DataTable();
            dt.TableName = "T_MID_REFERENCE";
            var idCol_1 = dt.Columns.Add("FCUSTOMERID");
            idCol_1.DataType = typeof(long);
            var idCol_2 = dt.Columns.Add("FMATERIALID");
            idCol_2.DataType = typeof(long);
            var idCol_3 = dt.Columns.Add("FQTY");
            idCol_3.DataType = typeof(double);
            dt.BeginLoadData();
            foreach (JObject item in arry)
            {
                JArray arryEntry = JArray.Parse(item["data"].ToString());
                if (arryEntry != null && arryEntry.Count > 0)
                {
                    foreach (var ob in arryEntry)
                    {
                        dt.LoadDataRow(new object[] {Convert.ToInt64(ob["customerid"].ToString()),Convert.ToUInt64(ob["materialid"].ToString()),Convert.ToDouble(ob["qty"].ToString())  }, true);
                    }
                }
            }
            dt.EndLoadData();
            // 批量插入到数据库
            DBServiceHelper.BulkInserts(ctx, string.Empty, string.Empty, dt);
        }
    }
}
