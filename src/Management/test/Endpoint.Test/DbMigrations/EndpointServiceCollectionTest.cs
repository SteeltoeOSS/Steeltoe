// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.DbMigrations;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddEntityFrameworkActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();

        services.AddDbMigrationsActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<DbMigrationsEndpointOptions>>();
        options.CurrentValue.Id.Should().Be("dbmigrations");
        var handler = serviceProvider.GetService<IDbMigrationsEndpointHandler>();
        handler.Should().NotBeNull();
    }
}
