using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Contracts
{
    /// <summary>
    /// api 接口实现类
    /// </summary>
    public class WebApiClient
    {
        private const string _serviceUrl = "http://api.kingdee.com/model/query?client_id={0}&client_secret={1}";
        CookieContainer cookies;
        Encoding encode;
        HttpWebRequest httpReq;
        public WebApiClient()
        {
            cookies = new CookieContainer();
            encode = Encoding.UTF8;
        }

        private void CreateRequest(string client_id, string client_secret)
        {
            var url = GetUri(client_id, client_secret);
            httpReq = (HttpWebRequest)HttpWebRequest.Create(url);
            httpReq.Method = "POST";
            httpReq.KeepAlive = true;
            httpReq.ContentType = "application/json";
            httpReq.Timeout = 300000;
            httpReq.CookieContainer = cookies;
        }

        public string SendRequest(string client_id, string clientSecret, string data)
        {
            CreateRequest(client_id, clientSecret);
            byte[] bytes = encode.GetBytes(data);
            using (Stream stream = httpReq.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                HttpWebResponse response = httpReq.GetResponse() as HttpWebResponse;
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), encode))
                {
                    var result = reader.ReadToEnd();
                    return result;
                }
            }
        }

        private static Uri GetUri(string client_id, string client_secret)
        {
            var url = new Uri(string.Format(_serviceUrl, client_id, client_secret));
            return url;
        }
    }
}
