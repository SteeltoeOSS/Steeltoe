// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.CloudFoundry;

public static class CloudFoundryConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder configurationBuilder)
    {
        return AddCloudFoundry(configurationBuilder, null, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder configurationBuilder, ICloudFoundrySettingsReader? settingsReader)
    {
        return AddCloudFoundry(configurationBuilder, settingsReader, NullLoggerFactory.Instance);
    }

    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder configurationBuilder, ICloudFoundrySettingsReader? settingsReader,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var source = new CloudFoundryConfigurationSource(settingsReader);
        return configurationBuilder.Add(source);
    }
}
