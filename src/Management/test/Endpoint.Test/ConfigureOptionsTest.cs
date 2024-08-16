// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.Environment;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ConfigureOptionsTest
{
    [Fact]
    public void Does_not_register_options_configurer_multiple_times()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddInfoActuator();
        services.AddEnvironmentActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IConfigureOptions<ManagementOptions>[] configurers = serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>().ToArray();
        configurers.Should().HaveCount(1);

        IOptionsChangeTokenSource<ManagementOptions>[] tokenSources = serviceProvider.GetServices<IOptionsChangeTokenSource<ManagementOptions>>().ToArray();
        tokenSources.Should().HaveCount(1);
    }

    [Fact]
    public void Can_register_additional_options_configurer_upfront()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddTransient<IConfigureOptions<ManagementOptions>, CustomManagementOptionsConfigurer>();
        services.AddInfoActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IConfigureOptions<ManagementOptions>[] configurers = serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>().ToArray();
        configurers.Should().HaveCount(2);
        configurers.OfType<ConfigureManagementOptions>().Should().HaveCount(1);
        configurers.OfType<CustomManagementOptionsConfigurer>().Should().HaveCount(1);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();
        optionsMonitor.CurrentValue.Port.Should().Be("9999");
    }

    [Fact]
    public void Can_register_additional_options_configurer_afterwards()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddInfoActuator();
        services.AddTransient<IConfigureOptions<ManagementOptions>, CustomManagementOptionsConfigurer>();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IConfigureOptions<ManagementOptions>[] configurers = serviceProvider.GetServices<IConfigureOptions<ManagementOptions>>().ToArray();
        configurers.Should().HaveCount(2);
        configurers.OfType<ConfigureManagementOptions>().Should().HaveCount(1);
        configurers.OfType<CustomManagementOptionsConfigurer>().Should().HaveCount(1);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ManagementOptions>>();
        optionsMonitor.CurrentValue.Port.Should().Be("9999");
    }

    private sealed class CustomManagementOptionsConfigurer : IConfigureOptions<ManagementOptions>
    {
        public void Configure(ManagementOptions options)
        {
            options.Port = "9999";
        }
    }
}
