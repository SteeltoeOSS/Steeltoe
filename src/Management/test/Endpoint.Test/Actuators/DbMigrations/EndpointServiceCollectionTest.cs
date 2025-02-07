// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.Actuators.DbMigrations;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddEntityFrameworkCoreActuator_AddsCorrectServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddDbMigrationsActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var options = serviceProvider.GetRequiredService<IOptionsMonitor<DbMigrationsEndpointOptions>>();
        options.CurrentValue.Id.Should().Be("dbmigrations");
        var handler = serviceProvider.GetService<IDbMigrationsEndpointHandler>();
        handler.Should().NotBeNull();
    }
}
