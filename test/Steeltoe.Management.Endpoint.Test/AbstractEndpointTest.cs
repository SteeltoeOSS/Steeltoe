using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steeltoe.Management.Endpoint.Security;
using Xunit;


namespace Steeltoe.Management.Endpoint.Test
{
    public class AbstractEndpointTest : BaseTest
    {
        [Fact]
        public void ThrowsIfOptionsull()
        {
            Assert.Throws<ArgumentNullException>(() => new TestEndpoint(null));
        }

        [Fact]
        public void ReturnsOptionValues()
        {
            var ep = new TestEndpoint(new TestOptions() { Id = "foo", Enabled = false, Sensitive = false });
            Assert.False(ep.Sensitive);
            Assert.False(ep.Enabled);
            Assert.Equal("foo", ep.Id);
            Assert.Equal(0, ep.Invoke());
        }

    }

    class TestEndpoint : AbstractEndpoint<int>
    {

        public TestEndpoint(IEndpointOptions opts) : base(opts)
        {

        }
    }

    class TestOptions : IEndpointOptions
    {
        public string Id { get; set; }
        public bool Enabled { get; set; }
        public bool Sensitive { get; set; }
        public IManagementOptions Global { get; set; }
        public string Path { get; set; }
        public Permissions RequiredPermissions { get; set; }
        public bool IsAccessAllowed(Permissions permissions)
        {
            return false;
        }
    }
}
