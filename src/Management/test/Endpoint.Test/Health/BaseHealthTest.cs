// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Health;

namespace Steeltoe.Management.Endpoint.Test.Health;

public class BaseHealthTest:BaseTest
{
    protected static IOptionsMonitor<HealthEndpointOptions> GetOptionsMonitorFromSettings() => GetOptionsMonitorFromSettings(new Dictionary<string, string>());
    protected static IOptionsMonitor<HealthEndpointOptions> GetOptionsMonitorFromSettings(Dictionary<string, string> appsettings)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureOptions<ConfigureHealthEndpointOptions>();

        var provider = services.BuildServiceProvider();
        var opts = provider.GetService<IOptionsMonitor<HealthEndpointOptions>>();
        return opts;
    }
    protected static HealthEndpointOptions GetOptionsFromSettings() => GetOptionsMonitorFromSettings().CurrentValue;

    protected static HealthEndpointOptions GetOptionsFromSettings(Dictionary<string, string> appSettings) => GetOptionsMonitorFromSettings(appSettings).CurrentValue;
}
