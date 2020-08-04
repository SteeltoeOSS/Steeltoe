// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Info;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoEndpointTest : BaseTest
    {
        [Fact]
        public void Invoke_NoContributors_ReturnsExpectedInfo()
        {
            var opts = new InfoEndpointOptions();
            var contributors = new List<IInfoContributor>();
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();
            Assert.NotNull(info);
            Assert.Empty(info);
        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            var opts = new InfoEndpointOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(), new TestContrib() };
            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                var tc = (TestContrib)contrib;
                Assert.True(tc.Called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions()
        {
            var opts = new InfoEndpointOptions();
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(true), new TestContrib() };

            var ep = new InfoEndpoint(opts, contributors, GetLogger<InfoEndpoint>());

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                var tc = (TestContrib)contrib;
                if (tc.Throws)
                {
                    Assert.False(tc.Called);
                }
                else
                {
                    Assert.True(tc.Called);
                }
            }
        }
    }
}
