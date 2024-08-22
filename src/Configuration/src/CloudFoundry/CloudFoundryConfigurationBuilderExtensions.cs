// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Logging;

namespace Steeltoe.Configuration.CloudFoundry;

public static class CloudFoundryConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds the JSON in the Cloud Foundry environment variables, such as VCAP_APPLICATION and VCAP_SERVICES, to the application configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder builder)
    {
        return AddCloudFoundry(builder, null, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds the JSON from the provided settings reader to the application configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="settingsReader">
    /// Provides access to the contents of the various Cloud Foundry environment variables.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder builder, ICloudFoundrySettingsReader? settingsReader)
    {
        return AddCloudFoundry(builder, settingsReader, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Adds the JSON from the provided settings reader to the application configuration.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IConfigurationBuilder" /> to add configuration to.
    /// </param>
    /// <param name="settingsReader">
    /// Provides access to the contents of the various Cloud Foundry environment variables.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging, or <see cref="BootstrapLoggerFactory.Default" /> to
    /// write only to the console until logging is fully initialized.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder builder, ICloudFoundrySettingsReader? settingsReader,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (!builder.Sources.OfType<CloudFoundryConfigurationSource>().Any())
        {
            var source = new CloudFoundryConfigurationSource(settingsReader);
            builder.Add(source);
        }

        return builder;
    }
}
