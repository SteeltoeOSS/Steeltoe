using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test
{
    public class CloudFoundryEndpointTest : BaseTest
    {
        [Fact]
        public void InvokeReturnsExpectedLinks()
        {
            var infoOpts = new InfoOptions();
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar",info._links["self"].href);
            Assert.True(info._links.ContainsKey("info"));
            Assert.Equal("http://localhost:5000/foobar/info", info._links["info"].href);
            Assert.Equal(2, info._links.Count);
        }

        [Fact]
        public void InvokeReturnsExpectedLinks2()
        {
            var cloudOpts = new CloudFoundryOptions();

            var ep = new CloudFoundryEndpoint(cloudOpts);

            var info = ep.Invoke("http://localhost:5000/foobar");
            Assert.NotNull(info);
            Assert.NotNull(info._links);
            Assert.True(info._links.ContainsKey("self"));
            Assert.Equal("http://localhost:5000/foobar", info._links["self"].href);
            Assert.Equal(1, info._links.Count);
        }
    }
}
