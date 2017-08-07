using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoEndpointTest : BaseTest
    {

        [Fact]
        public void InvokeReturnsExpectedInfo()
        {

            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>();
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();
            Assert.NotNull(info);
            Assert.Equal(0, info.Count);

        }

        [Fact]
        public void InvokeCallsAllContributors()
        {
            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(), new TestContrib() };
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach(var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                Assert.True(tc.called);
            }
        }

        [Fact]
        public void InvokeHandlesExceptions()
        {
            var opts = new InfoOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(true), new TestContrib() };
      
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                if (tc.throws)
                    Assert.False(tc.called);
                else
                    Assert.True(tc.called);
            }
        }
    }

    class TestContrib : IInfoContributor
    {
        public bool called = false;
        public bool throws = false;
        public TestContrib()
        {
            this.throws = false;
        }
        public TestContrib(bool throws)
        {
            this.throws = throws;
        }
        public void Contribute(IInfoBuilder builder)
        {
            if (throws)
            {
                throw new Exception();
            }
            called = true;
        }
    }
}
