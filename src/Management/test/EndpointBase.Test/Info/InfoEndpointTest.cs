// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Info;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoEndpointTest : BaseTest
    {
        private readonly ITestOutputHelper _output;

        public InfoEndpointTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Invoke_NoContributors_ReturnsExpectedInfo()
        {
            using var tc = new TestContext(_output);
            var contributors = new List<IInfoContributor>();

            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddInfoActuatorServices(configuration);
                services.AddSingleton<IEnumerable<IInfoContributor>>(contributors);
            };

            var ep = tc.GetService<IInfoEndpoint>();

            var info = ep.Invoke();
            Assert.NotNull(info);
            Assert.Empty(info);
        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            using var tc = new TestContext(_output);
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(), new TestContrib() };

            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddInfoActuatorServices(configuration);
                services.AddSingleton<IEnumerable<IInfoContributor>>(contributors);
            };

            var ep = tc.GetService<IInfoEndpoint>();

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                var tcc = (TestContrib)contrib;
                Assert.True(tcc.Called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions()
        {
            using var tc = new TestContext(_output);
            var contributors = new List<IInfoContributor>() { new TestContrib(), new TestContrib(true), new TestContrib() };

            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddInfoActuatorServices(configuration);
                services.AddSingleton<IEnumerable<IInfoContributor>>(contributors);
            };

            var ep = tc.GetService<IInfoEndpoint>();

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                var tcc = (TestContrib)contrib;
                if (tcc.Throws)
                {
                    Assert.False(tcc.Called);
                }
                else
                {
                    Assert.True(tcc.Called);
                }
            }
        }
    }
}
