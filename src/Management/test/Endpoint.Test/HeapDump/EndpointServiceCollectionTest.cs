// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.HeapDump.Test;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddHeapDumpActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddHeapDumpActuator());
        Assert.Contains(nameof(services), ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddHeapDumpActuator());
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddHeapDumpActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:heapdump:enabled"] = "false",
            ["management:endpoints:heapdump:HeapDumpType"] = "Normal"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot config = configurationBuilder.Build();

        services.AddHeapDumpActuator(config);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IHeapDumpOptions>();
        Assert.NotNull(options);
        Assert.Equal("Normal", options.HeapDumpType);
        var repo = serviceProvider.GetService<IHeapDumper>();
        Assert.NotNull(repo);
        var ep = serviceProvider.GetService<HeapDumpEndpoint>();
        Assert.NotNull(ep);
    }
}
