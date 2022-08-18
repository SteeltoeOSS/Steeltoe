// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

/// <summary>
/// Extension methods for adding <see cref="ConfigServerConfigurationProvider" />.
/// </summary>
public static class ConfigServerConfigurationBuilderExtensionsCore
{
    public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment,
        ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(environment);

        return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
    }

    private static IConfigurationBuilder DoAddConfigServer(IConfigurationBuilder configurationBuilder, string applicationName, string environmentName,
        ILoggerFactory logFactory)
    {
        ArgumentGuard.NotNull(configurationBuilder);

        var settings = new ConfigServerClientSettings
        {
            Name = applicationName,
            Environment = environmentName
        };

        return configurationBuilder.AddConfigServer(settings, logFactory);
    }
}
