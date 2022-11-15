// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Loggers;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Loggers;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddLoggersActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddLoggersActuator());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => services2.AddLoggersActuator());
    }

    [Fact]
    public void AddLoggersActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "true",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configurationRoot);
            builder.AddDynamicConsole();
        });

        services.AddLoggersActuator(configurationRoot);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetService<ILoggersOptions>();
        var ep = serviceProvider.GetService<LoggersEndpoint>();

        Assert.NotNull(options);
        Assert.NotNull(ep);
    }
}
