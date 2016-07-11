using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SteelToe.CloudFoundry.Connector.Services
{
    public class EurekaServiceInfo : UriServiceInfo 
    {
        public string ClientId { get; internal set; }
        public string ClientSecret { get; internal set; }
        public string TokenUri { get; internal set; }

        public EurekaServiceInfo(string id, string uri, string clientId, string clientSecret, string tokenUri) :
            base(id, uri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            TokenUri = tokenUri;
        }
    }
}
