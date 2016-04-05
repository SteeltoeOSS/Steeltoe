using SteelToe.Discovery.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fortune_Teller_UI.Services
{
    public class FortuneService : IFortuneService
    {
        DiscoveryHttpClientHandler _handler;

        private const string RANDOM_FORTUNE_URL = "http://fortuneService/api/fortunes/random";

        public FortuneService(IDiscoveryClient client)
        {
            _handler = new DiscoveryHttpClientHandler(client);
        }

        public async Task<string> RandomFortuneAsync()
        {
            var client = GetClient();
            return await client.GetStringAsync(RANDOM_FORTUNE_URL);
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient(_handler, false);
            return client;
        }
    }
}
