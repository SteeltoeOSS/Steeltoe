// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Connectors.Services;
using Steeltoe.Discovery.Client;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class TestDiscoveryClientExtension : IDiscoveryClientExtension
{
    /// <inheritdoc />
    public bool IsConfigured(IConfiguration configuration, IServiceInfo? serviceInfo)
    {
        ArgumentGuard.NotNull(configuration);

        return configuration.GetValue<bool>("testdiscovery:enabled");
    }

    /// <inheritdoc />
    public void ApplyServices(IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddOptions<TestDiscoveryClientOptions>()
            .Configure<IConfiguration>((options, configuration) => configuration.GetSection("testdiscovery").Bind(options));

        services.AddSingleton<TestDiscoveryClient>();
        services.TryAddSingleton<IDiscoveryClient>(serviceProvider => serviceProvider.GetRequiredService<TestDiscoveryClient>());
    }
}
