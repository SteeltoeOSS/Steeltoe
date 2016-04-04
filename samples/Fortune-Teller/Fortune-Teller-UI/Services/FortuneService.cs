using SteelToe.Discovery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fortune_Teller_UI.Services
{
    public class FortuneService : IFortuneService
    {
        DiscoveryHttpClientHandler _handler;

        public FortuneService(IDiscoveryClient client)
        {
            _handler = new DiscoveryHttpClientHandler(client);
        }

        public async Task<string> RandomFortuneAsync()
        {
            var client = GetClient();
            return await client.GetStringAsync("http://fortuneService/api/fortunes/random");
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient(_handler, false);
         
            return client;
        }
    }
}
