// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddThreadDumpActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:dump:enabled"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddThreadDumpActuator();

        services.AddSingleton(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ThreadDumpEndpointOptions>>();
        Assert.False(options.CurrentValue.Enabled);

        var threadDumper = serviceProvider.GetService<EventPipeThreadDumper>();
        Assert.NotNull(threadDumper);

        var handler = serviceProvider.GetService<IThreadDumpEndpointHandler>();
        Assert.NotNull(handler);
    }
}
