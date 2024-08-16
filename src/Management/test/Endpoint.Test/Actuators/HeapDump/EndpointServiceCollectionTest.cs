// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddHeapDumpActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:heapdump:enabled"] = "false",
            ["management:endpoints:heapdump:HeapDumpType"] = "Normal"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHeapDumpActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var heapDumper = serviceProvider.GetService<HeapDumper>();
        Assert.NotNull(heapDumper);
        var handler = serviceProvider.GetService<IHeapDumpEndpointHandler>();
        Assert.NotNull(handler);
    }
}
