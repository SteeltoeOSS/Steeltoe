// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class HeapDumpEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_CreatesDump()
    {
        using var testContext = new TestContext(_testOutputHelper);

        IOptionsMonitor<HeapDumpEndpointOptions> options = GetOptionsMonitorFromSettings<HeapDumpEndpointOptions, ConfigureHeapDumpEndpointOptions>();

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHeapDumpActuator();
            services.AddSingleton(serviceProvider => new HeapDumper(options, serviceProvider.GetRequiredService<ILogger<HeapDumper>>()));
        };

        var handler = testContext.GetRequiredService<IHeapDumpEndpointHandler>();

        string? result = await handler.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        File.Delete(result);
    }
}
