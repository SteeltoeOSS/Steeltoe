using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class EndpointApplicationBuilderExtensionsTest : BaseTest
    {
        [Fact]
        public void UseInfoActuatorThrowsIfNulls()
        {
            IApplicationBuilder builder = null;
            IApplicationBuilder builder2 = new ApplicationBuilder(null);
            IConfiguration config = null;

            Assert.Throws<ArgumentNullException>(() => builder.UseInfoActuator(config));
            Assert.Throws<ArgumentNullException>(() => builder2.UseInfoActuator(config));
        }

        [Fact]
        public void UseInfoActuatorAddsCorrectMiddleware()
        {
            IApplicationBuilder builder = new ApplicationBuilder(null);
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/management',
            'info' : {
                'enabled': false,
                'sensitive': false,
                'id': 'infomanagement'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            builder.UseInfoActuator(config);

            Assert.True(builder.Properties.Count > 0);

        }
        [Fact]
        public async void InfoActuatorReturnsExpectedData()
        {
            // Note: This test pulls in from git.properties and appsettings created 
            // in the Startup class

            var builder = new WebHostBuilder().UseStartup<Startup>();
            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                var result = await client.GetAsync("http://localhost/management/infomanagement");
                Assert.Equal(HttpStatusCode.OK, result.StatusCode);
                var json = await result.Content.ReadAsStringAsync();
                Assert.NotNull(json);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string,object>>>(json);
                Assert.NotNull(dict);

                Assert.Equal(3, dict.Count);
                Assert.True(dict.ContainsKey("application"));
                Assert.True(dict.ContainsKey("NET"));
                Assert.True(dict.ContainsKey("git"));

                var appNode = dict["application"] as Dictionary<string, object>;
                Assert.NotNull(appNode);
                Assert.Equal("foobar", appNode["name"]);

                var netNode = dict["NET"] as Dictionary<string, object>;
                Assert.NotNull(netNode);
                Assert.Equal("Core", netNode["type"]);

                var gitNode = dict["git"] as Dictionary<string, object>;
                Assert.NotNull(gitNode);
                Assert.True(gitNode.ContainsKey("build"));
                Assert.True(gitNode.ContainsKey("branch"));
                Assert.True(gitNode.ContainsKey("commit"));
                Assert.True(gitNode.ContainsKey("closest"));
                Assert.True(gitNode.ContainsKey("dirty"));
                Assert.True(gitNode.ContainsKey("remote"));
                Assert.True(gitNode.ContainsKey("tags"));

            }
        }
    }

}
