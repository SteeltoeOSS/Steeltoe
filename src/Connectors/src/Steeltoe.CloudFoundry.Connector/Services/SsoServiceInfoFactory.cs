using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class SsoServiceInfoFactory : ServiceInfoFactory
    {

        public SsoServiceInfoFactory() : base(new Tags("p-identity"), "uaa")
        {
        }
   
        public override IServiceInfo Create(Service binding)
        {
            string clientId = GetClientIdFromCredentials(binding.Credentials);
            string clientSecret = GetClientSecretFromCredentials(binding.Credentials);
            string authDomain = GetStringFromCredentials(binding.Credentials, "auth_domain");
            string uri = GetUriFromCredentials(binding.Credentials);

            if (!string.IsNullOrEmpty(authDomain))
            {
                return new SsoServiceInfo(binding.Name, clientId, clientSecret, authDomain);
            }

            if (!string.IsNullOrEmpty(uri))
            {
                return new SsoServiceInfo(binding.Name, clientId, clientSecret, UpdateUaaScheme(uri));
            }
            return null;
        }

        internal string UpdateUaaScheme(string uaaString)
        {
            if (uaaString.StartsWith("uaa:"))
            {
                return "https:" + uaaString.Substring(4);
            }

            return uaaString;
        }
    
    }
}
