using Kingdee.BOS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KEEPER.K3.TLMB.Contracts
{
    public class TLMBNewServiceFactory:ServiceFactory
    {
        static bool notRegistered = true;
        static TLMBNewServiceFactory()
        {
            RegisterService();
        }

        /// <summary>
        /// 注册服务，以便在调用MyServiceFactory.GetService时，能够顺利的反射创建出接口实例
        /// </summary>
        public static new void RegisterService()
        {
            if (notRegistered)
            {
                _mapServer.Add(typeof(ICommonService).AssemblyQualifiedName,
                    "KEEPER.K3.App.CommonService,KEEPER.K3.App.CommonService");
                notRegistered = false;
            }
            Kingdee.BOS.Contracts.ServiceFactory.RegisterService();
        }

    }
}
