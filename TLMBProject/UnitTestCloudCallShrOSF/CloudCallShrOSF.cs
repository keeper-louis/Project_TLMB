using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using Kingdee.BOS.Resource;
using System.Web;

namespace UnitTestCloudCallShrOSF
{
    [TestClass]
    public class CloudCallShrOSF
    {
        private HttpClient _httpclinet = new HttpClient();
        private string _shrSecure;
        private int _shrTime;
        private string _shrUser;
        private string shrUrl;
        [TestMethod]
        public void TestMethod1()
        {
            //using (IDataReader reader = DBServiceHelper.ExecuteReader(base.Context, "select FSHTURL, FSHRUSER, FSYNCCOUNT, FSHRSECRET, FSHRTIME from T_SEC_SIMPLEPASSPORT_S where FID = 1"))
            //{
            //    if (reader.Read())
            //    {
            //        this.shrUrl = reader["FSHTURL"].ToString();
            //        this._shrUser = reader["FSHRUSER"].ToString();
            //        this._shrTime = Convert.ToInt32(reader["FSHRTIME"]);
            //        this._shrSecure = reader["FSHRSECRET"].ToString();
            //        object obj2 = reader["FSYNCCOUNT"];
            //        this.singleZise = (obj2 == null) ? 100 : Convert.ToInt32(reader["FSYNCCOUNT"]);
            //        if (this.singleZise == 0)
            //        {
            //            this.singleZise = 100;
            //        }
            //    }
            //}
            this.shrUrl = "http://221.180.236.23:7888/shr/";
            this._shrUser = "w";
            this._shrTime = 40;
            this._shrSecure = "erewqreqr876";
            this._httpclinet.Url = this.shrUrl;
            if (this.DealWithLoginResult(this._httpclinet.Login(this._shrUser, this._shrSecure, this._shrTime)))
            {
                string url = string.Format("/shr/msf/service.do?method=callService&serviceName=saveTripBill&tripStartPlace=sz&tripEndPlace=sh&tripType=001&tripStartTime=2018-12-2508:00:00&tripEndTime=2018-12-2518:00:00&tripDays=1&tripReason=t&personNum=TZ00000017");
                //string url = "/shrosf.jsp?serviceName=getAttendanceFiles&param={'rows':'100','page':'1','transmitStartTime':'1999-01-01','flag':'add'}";//考勤
                RequestResult result = this._httpclinet.SysncRequest(url, "");
            }
        }

        private bool DealWithLoginResult(RequestResult requestResult)
        {
            if (requestResult.IsError)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine(ResManager.LoadKDString("1. 请确保网络正常，且务必支持POST请求（可联系网络管理员协助检查）", "001005000007236", SubSystemType.BASE, new object[0]));
                builder.AppendLine(ResManager.LoadKDString("2. 请检查修复s-HR登录接口问题（可联系s-HR系统管理员或者s-HR服务支持人员协助）", "001005000007241", SubSystemType.BASE, new object[0]));
                builder.AppendLine(ResManager.LoadKDString("s-HR抛出异常如下：", "001005000007238", SubSystemType.BASE, new object[0]));
                builder.AppendLine(requestResult.Content);
                //this.View.ShowWarnningMessage(builder.ToString(), ResManager.LoadKDString("无法登录s-HR系统！", "001005000005058", SubSystemType.BASE, new object[0]), MessageBoxOptions.OK, null, MessageBoxType.Advise);
            }
            return !requestResult.IsError;
        }
    }
}
