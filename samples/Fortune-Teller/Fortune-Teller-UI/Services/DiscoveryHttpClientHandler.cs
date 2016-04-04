
using SteelToe.Discovery.Client;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fortune_Teller_UI.Services
{
    public class DiscoveryHttpClientHandler : HttpClientHandler
    {
        private IDiscoveryClient _client;

        public DiscoveryHttpClientHandler(IDiscoveryClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var current = request.RequestUri;
            try {
                request.RequestUri = LookupService(current);
                return await base.SendAsync(request, cancellationToken);
            } finally
            {
                request.RequestUri = current;
            }
        
        }

        private Uri LookupService(Uri current)
        {
            if (!current.IsDefaultPort)
            {
                return current;
            }

            var instances = _client.GetInstances(current.Host);
            if (instances.Count > 0)
            {
                return new Uri(instances[0].Uri, current.PathAndQuery);
            }

            return current;
             
        }
    }
}
