// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.Logfile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddLogFileActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestHostEnvironmentFactory.Create());

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:logfile:path"] = "/some",
            ["management:endpoints:logfile:filePath"] = "/var/logs/app.log"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddLogFileActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var options = serviceProvider.GetRequiredService<IOptionsMonitor<LogFileEndpointOptions>>();
        Assert.Equal("/some", options.CurrentValue.Path);

        var handler = serviceProvider.GetService<ILogFileEndpointHandler>();
        Assert.NotNull(handler);
    }
}
