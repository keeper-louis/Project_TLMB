using Kingdee.BOS.WebApi.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Contracts
{
    /// <summary>
    /// api 接口调用工厂类
    /// </summary>
    public class ClientFactory
    {
        WebApiClient client;

        public ClientFactory()
        {
            if (client == null)
            {
                client = new WebApiClient();
            }
        }
        public T execute<T>(string client_id, string client_secret, JObject paramValues)
        {
            string fmtParams = JsonConvert.SerializeObject(paramValues);
            var result = client.SendRequest(client_id, client_secret, fmtParams);
            return JsonObject.Deserialize<T>(result);
        }
    }
}
