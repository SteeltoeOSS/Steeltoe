using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class TestConfigServerConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationProvider _provider;

        public TestConfigServerConfigurationSource(IConfigurationProvider provider)
        {
            _provider = provider;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => _provider;
    }
}
