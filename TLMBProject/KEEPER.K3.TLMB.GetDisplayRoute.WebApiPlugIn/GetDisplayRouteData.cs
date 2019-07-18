using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Core.Permission.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using KEEPER.K3.TLMB.Core;

namespace KEEPER.K3.TLMB.GetDisplayRoute.WebApiPlugIn
{
    [Description("获取陈列地图数据权限范围")]
    public class GetDisplayRouteData : AbstractWebApiBusinessService
    {
        public GetDisplayRouteData(KDServiceContext context) : base(context)
        {
        }
        public Context Ctx
        {
            get
            {
                return this.KDContext.Session.AppContext;
            }
        }

        public string getData(string parameterJson)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(parameterJson);
            long personId = Convert.ToInt64(jo.Root["personId"].ToString());
            string strSql = string.Format(@"/*dialect*/SELECT DEPTID,DEPTNAME FROM TABLE{0}", personId);
            DynamicObjectCollection doo = DBUtils.ExecuteDynamicObject(Ctx, strSql);
            List<Dept> deptList = new List<Dept>();
            foreach (DynamicObject item in doo)
            {
                Dept dept = new Dept();
                dept.deptId = Convert.ToInt64(item["DEPTID"]);
                dept.deptname = Convert.ToString(item["DEPTNAME"]);
                deptList.Add(dept);
            }
            string result = JsonConvert.SerializeObject(deptList);
            return result;
        }
    }
}
