using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class EurekaServiceInfoFactory : ServiceInfoFactory
    {
        public EurekaServiceInfoFactory()
            : base(new Tags("eureka"), new string[0])
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            string uri = GetUriFromCredentials(binding.Credentials);
            string clientId = GetClientIdFromCredentials(binding.Credentials);
            string clientSecret = GetClientSecretFromCredentials(binding.Credentials);
            string accessTokenUri = GetAccessTokenUriFromCredentials(binding.Credentials);

            return new EurekaServiceInfo(binding.Name, uri, clientId, clientSecret, accessTokenUri);
        }
    }
}
