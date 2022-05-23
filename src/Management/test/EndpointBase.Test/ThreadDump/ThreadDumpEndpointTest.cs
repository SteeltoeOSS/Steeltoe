// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.ThreadDump.Test
{
    public class ThreadDumpEndpointTest : BaseTest
    {
        private readonly ITestOutputHelper _output;

        public ThreadDumpEndpointTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Constructor_ThrowsIfNullRepo()
        {
            Assert.Throws<ArgumentNullException>(() => new ThreadDumpEndpoint(new ThreadDumpEndpointOptions(), null));
        }

        [Fact]
        public void Invoke_CallsDumpThreads()
        {
            using var tc = new TestContext(_output);
            var dumper = new TestThreadDumper();
            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddSingleton<IThreadDumper>(dumper);
                services.AddThreadDumpActuatorServices(configuration, MediaTypeVersion.V1);
            };

            var ep = tc.GetService<IThreadDumpEndpoint>();
            var result = ep.Invoke();
            Assert.NotNull(result);
            Assert.True(dumper.DumpThreadsCalled);
        }
    }
}
