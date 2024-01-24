// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Discovery.Client.SimpleClients;

internal sealed class ConfigurationDiscoveryClientExtension : IDiscoveryClientExtension
{
    private const string ConfigurationPrefix = "discovery";

    /// <inheritdoc />
    public bool IsConfigured(IConfiguration configuration, IServiceInfo? serviceInfo)
    {
        ArgumentGuard.NotNull(configuration);

        return configuration.GetSection(ConfigurationPrefix).GetChildren().Any();
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddOptions<ConfigurationDiscoveryOptions>()
            .Configure<IConfiguration>((options, configuration) => configuration.GetSection(ConfigurationPrefix).Bind(options));

        services.AddSingleton<IDiscoveryClient>(serviceProvider =>
            new ConfigurationDiscoveryClient(serviceProvider.GetRequiredService<IOptionsMonitor<ConfigurationDiscoveryOptions>>()));
    }
}
