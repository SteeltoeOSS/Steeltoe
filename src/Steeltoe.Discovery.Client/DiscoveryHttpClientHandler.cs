
using Steeltoe.Discovery.Client;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Client
{
    public class DiscoveryHttpClientHandler : DiscoveryHttpClientHandlerBase
    {
        private IDiscoveryClient _client;

        public DiscoveryHttpClientHandler(IDiscoveryClient client) : base()
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        internal protected override Uri LookupService(Uri current)
        {
            if (!current.IsDefaultPort)
            {
                return current;
            }
      
            var instances = _client.GetInstances(current.Host);
            if (instances.Count > 0)
            {
                int indx = _random.Next(instances.Count);
                return new Uri(instances[indx].Uri, current.PathAndQuery);
            }

            return current;
             
        }
    }
}
