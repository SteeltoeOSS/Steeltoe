using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class SsoServiceInfo : ServiceInfo
    {
        public string ClientId { get; internal set; }

        public string ClientSecret { get; internal set; }

        public string AuthDomain { get; internal set; }

        public SsoServiceInfo(string id, string clientId, string clientSecret, string domain)
            : base(id)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AuthDomain = domain;
        }
    }
}
