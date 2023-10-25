// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;

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
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging();
        services.AddThreadDumpActuator();

        services.AddSingleton<IConfiguration>(configurationRoot);
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetService<IOptionsMonitor<ThreadDumpEndpointOptions>>();
        Assert.NotNull(options);
        var threadDumper = serviceProvider.GetService<EventPipeThreadDumper>();
        Assert.NotNull(threadDumper);
        var handler = serviceProvider.GetService<IThreadDumpEndpointHandler>();
        Assert.NotNull(handler);
    }
}
