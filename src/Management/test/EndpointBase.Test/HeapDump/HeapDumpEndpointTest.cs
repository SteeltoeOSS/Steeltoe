// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.HeapDump.Test;

public class HeapDumpEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public HeapDumpEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsIfNullRepo()
    {
        Assert.Throws<ArgumentNullException>(() => new HeapDumpEndpoint(new HeapDumpEndpointOptions(), null));
    }

    [Fact]
    public void Invoke_CreatesDump()
    {
        if (Platform.IsWindows && RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.InvariantCultureIgnoreCase))
        {
            using var tc = new TestContext(_output);

            tc.AdditionalServices = (services, configuration) =>
            {
                services.AddHeapDumpActuatorServices(configuration);
                services.AddSingleton<IHeapDumper>(sp => new HeapDumper(new HeapDumpEndpointOptions(), logger: sp.GetRequiredService<ILogger<HeapDumper>>()));
            };

            var ep = tc.GetService<IHeapDumpEndpoint>();

            string result = ep.Invoke();
            Assert.NotNull(result);
            Assert.True(File.Exists(result));
            File.Delete(result);
        }
        else if (!Platform.IsOSX)
        {
            if (typeof(object).Assembly.GetType("System.Index") != null)
            {
                using var tc = new TestContext(_output);

                tc.AdditionalServices = (services, configuration) =>
                {
                    services.AddHeapDumpActuatorServices(configuration);

                    services.AddSingleton<IHeapDumper>(sp =>
                        new HeapDumper(new HeapDumpEndpointOptions(), logger: sp.GetRequiredService<ILogger<HeapDumper>>()));
                };

                var ep = tc.GetService<IHeapDumpEndpoint>();

                string result = ep.Invoke();
                Assert.NotNull(result);
                Assert.True(File.Exists(result));
                File.Delete(result);
            }
        }
        else if (Platform.IsWindows || Platform.IsLinux)
        {
            throw new Exception();
        }
    }
}
