using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using System;


namespace Steeltoe.Common.Http
{
    public class DiscoveryHttpClientHandler : DiscoveryHttpClientHandlerBase
    {
        private IDiscoveryClient _client;
        private ILogger<DiscoveryHttpClientHandler> _logger;

        public DiscoveryHttpClientHandler(IDiscoveryClient client, ILogger<DiscoveryHttpClientHandler> logger = null) : base()
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            _logger = logger;
        }

        public override Uri LookupService(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            var instances = _client.GetInstances(current.Host);
            if (instances.Count > 0)
            {
                int indx = _random.Next(instances.Count);
                current = new Uri(instances[indx].Uri, current.PathAndQuery);
            }
            _logger?.LogDebug("LookupService() returning {0} ", current.ToString());
            return current;

        }
    }
}


