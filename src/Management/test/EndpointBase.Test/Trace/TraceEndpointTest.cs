// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceEndpointTest : BaseTest
    {
        private readonly ITestOutputHelper _output;

        public TraceEndpointTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_ThrowsIfNullRepo()
        {
            Assert.Throws<ArgumentNullException>(() => new TraceEndpoint(new TraceEndpointOptions(), null));
        }

        [Fact]
        public void DoInvoke_CallsTraceRepo()
        {
            using (var tc = new TestContext(_output))
            {
                var repo = new TestTraceRepo();

                tc.AdditionalServices = (services, configuration) =>
                {
                    services.AddSingleton<ITraceRepository>(repo);
                    services.AddTraceActuatorServices(configuration, MediaTypeVersion.V1);
                };

                var ep = tc.GetService<ITraceEndpoint>();
                var result = ep.Invoke();
                Assert.NotNull(result);
                Assert.True(repo.GetTracesCalled);
            }
        }
    }
}
