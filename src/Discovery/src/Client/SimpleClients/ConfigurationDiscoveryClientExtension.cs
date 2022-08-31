// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Connector.Services;

namespace Steeltoe.Discovery.Client.SimpleClients;

public class ConfigurationDiscoveryClientExtension : IDiscoveryClientExtension
{
    public const string ConfigurationPrefix = "discovery:services";

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        services.AddOptions<List<ConfigurationServiceInstance>>()
            .Configure<IConfiguration>((options, configuration) => configuration.GetSection(ConfigurationPrefix).Bind(options));

        services.AddSingleton<IDiscoveryClient>(serviceProvider =>
            new ConfigurationDiscoveryClient(serviceProvider.GetRequiredService<IOptionsMonitor<List<ConfigurationServiceInstance>>>()));
    }

    public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
    {
        return configuration.GetSection(ConfigurationPrefix).GetChildren().Any();
    }
}
