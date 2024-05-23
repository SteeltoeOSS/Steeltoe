// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.HeapDump;

public sealed class HeapDumpEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public HeapDumpEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_CreatesDump()
    {
        IOptionsMonitor<HeapDumpEndpointOptions> options = GetOptionsMonitorFromSettings<HeapDumpEndpointOptions, ConfigureHeapDumpEndpointOptions>();

        if (Platform.IsWindows && RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase))
        {
            using var testContext = new TestContext(_output);

            testContext.AdditionalServices = (services, _) =>
            {
                services.AddHeapDumpActuatorServices();

                services.AddSingleton(sp => new HeapDumper(options, sp.GetRequiredService<ILogger<HeapDumper>>()));
            };

            var handler = testContext.GetRequiredService<IHeapDumpEndpointHandler>();

            string? result = await handler.InvokeAsync(null, CancellationToken.None);
            Assert.NotNull(result);
            Assert.True(File.Exists(result));
            File.Delete(result);
        }
        else if (!Platform.IsOSX)
        {
            if (typeof(object).Assembly.GetType("System.Index") != null)
            {
                using var testContext = new TestContext(_output);

                testContext.AdditionalServices = (services, _) =>
                {
                    services.AddHeapDumpActuatorServices();

                    services.AddSingleton(sp => new HeapDumper(options, sp.GetRequiredService<ILogger<HeapDumper>>()));
                };

                var handler = testContext.GetRequiredService<IHeapDumpEndpointHandler>();

                string? result = await handler.InvokeAsync(null, CancellationToken.None);
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
