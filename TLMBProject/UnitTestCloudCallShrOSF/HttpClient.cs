using Kingdee.BOS.KDThread;
using Kingdee.BOS.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace UnitTestCloudCallShrOSF
{
    public class HttpClient
    {
        // Fields
        private CookieContainer _cookieContainer;
        private bool _isLogin;
        private string _shrSecure;
        private int _shrTime;
        private string _strUser;
        private int errorRequest;

        // Methods
        private RequestResult Login()
        {
            this._cookieContainer = new CookieContainer();
            string url = string.Format("OTP2sso.jsp?username=" + this._strUser + "&password=" + HttpUtility.UrlEncode(HttpUtility.UrlEncode(Token.CreateToken(this._strUser, this._shrSecure, this._shrTime))) + "&userAuthPattern=OTP", new object[0]);
            Logger.Info("强制更新s-HR数据:登录：", url, true);
            this._isLogin = true;
            RequestResult result = this.SysncRequest(url, "");
            this._isLogin = false;
            Logger.Info("强制更新s-HR数据:登录结果：", string.Concat(new object[] { "是否有错误:", result.IsError, ",消息：", result.Content }), true);
            return result;
        }

        public RequestResult Login(string strUser, string shrSecure, int shrTime)
        {
            this._strUser = strUser;
            this._shrSecure = shrSecure;
            this._shrTime = shrTime;
            return this.Login();
        }

        private RequestResult Request(HttpWebRequest httpWebRequest, string url, string urlContent)
        {
            RequestResult result;
            bool flag = false;
            Label_0002:
            result = new RequestResult();
            HttpWebResponse response = null;
            Stream responseStream = null;
            StreamReader reader = null;
            result.IsError = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(urlContent) && !flag)
                {
                    flag = true;
                    byte[] bytes = Encoding.UTF8.GetBytes(urlContent);
                    httpWebRequest.Method = "POST";
                    httpWebRequest.ContentLength = bytes.Length;
                    using (Stream stream2 = httpWebRequest.GetRequestStream())
                    {
                        stream2.Write(bytes, 0, bytes.Length);
                        stream2.Close();
                    }
                }
                Logger.Info("强制更新s-HR数据：请求", this.Url + url, true);
                Logger.Info("强制更新s-HR数据：请求内容", urlContent, true);
                response = httpWebRequest.GetResponse() as HttpWebResponse;
                responseStream = response.GetResponseStream();
                reader = new StreamReader(responseStream);
                string str = reader.ReadToEnd();
                Logger.Info("强制更新s-HR数据：返回值", str, true);
                result.IsError = false;
                result.Content = str;
            }
            catch (Exception exception)
            {
                Logger.Error("强制更新s-HR数据", this.Url + url, exception);
                StringBuilder builder = new StringBuilder();
                MainWorker.BuildExceptionMessage(exception, builder);
                result.Content = builder.ToString();
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (responseStream != null)
                {
                    responseStream.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
            string content = result.Content;
            if ((result.IsError || string.IsNullOrWhiteSpace(content)) || ((content.Contains("window.logoutHR") || content.Contains("id=\"loginForm\"")) || content.Contains("{\"sucess\":\"false\",\"msgCode\":\"500\",\"errorMsg\":\"session 已失效\"}")))
            {
                if (this.errorRequest < 3)
                {
                    Thread.Sleep(0x3e8);
                    this.errorRequest++;
                    if (((string.IsNullOrWhiteSpace(content) || content.Contains("window.logoutHR")) || (content.Contains("id=\"loginForm\"") || content.Contains("{\"sucess\":\"false\",\"msgCode\":\"500\",\"errorMsg\":\"session 已失效\"}"))) && !this._isLogin)
                    {
                        this.Login();
                    }
                    goto Label_0002;
                }
                this.errorRequest = 0;
                result.IsError = true;
            }
            return result;
        }

        public RequestResult SysncRequest(string url, string content = "")
        {

            HttpWebRequest httpWebRequest = WebRequest.Create(this.Url + url + "&K3CloudGUID=" + Guid.NewGuid().ToString()) as HttpWebRequest;
            httpWebRequest.Timeout = 0x36ee80;
            httpWebRequest.AllowAutoRedirect = true;
            httpWebRequest.CookieContainer = this._cookieContainer;
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Accept = "application/json; charset=utf-8";
            //if (httpWebRequest.Referer==null)
            //{
            //    httpWebRequest.Referer = "http://221.180.236.23:7888/shr";
            //}
            return this.Request(httpWebRequest, url, content);
        }

        // Properties
        public string Url { get; set; }
    }
}
