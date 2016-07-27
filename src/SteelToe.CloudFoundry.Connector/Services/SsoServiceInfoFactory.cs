using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SteelToe.Extensions.Configuration.CloudFoundry;

namespace SteelToe.CloudFoundry.Connector.Services
{
    public class SsoServiceInfoFactory : ServiceInfoFactory
    {

        public SsoServiceInfoFactory() : base(new Tags("p-identity"), (string[]) null)
        {
        }
   
        public override IServiceInfo Create(Service binding)
        {
            string clientId = GetClientIdFromCredentials(binding.Credentials);
            string clientSecret = GetClientSecretFromCredentials(binding.Credentials);
            string authDomain = GetStringFromCredentials(binding.Credentials, "auth_domain");

            return new SsoServiceInfo(binding.Name, clientId, clientSecret, authDomain );
        }
    
    }
}
