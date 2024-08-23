// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Configuration.Placeholder;

/// <summary>
/// Configuration source used in creating a <see cref="PlaceholderResolverProvider" /> that resolves placeholders. A placeholder takes the form of:
/// <code><![CDATA[
/// ${some:config:reference?default_if_not_present}
/// ]]></code>
/// </summary>
internal sealed class PlaceholderResolverSource : IConfigurationSource
{
    private readonly IConfigurationRoot? _configuration;

    internal IList<IConfigurationSource>? Sources { get; }
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderResolverSource" /> class.
    /// </summary>
    /// <param name="sources">
    /// The configuration sources to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public PlaceholderResolverSource(IList<IConfigurationSource> sources, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Sources = sources.ToList();
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderResolverSource" /> class.
    /// </summary>
    /// <param name="root">
    /// The root configuration to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public PlaceholderResolverSource(IConfigurationRoot root, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _configuration = root;
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Builds a <see cref="PlaceholderResolverProvider" /> from the specified builder.
    /// </summary>
    /// <param name="builder">
    /// Used to build providers from sources, in case a list of sources was provided instead of a configuration root.
    /// </param>
    /// <returns>
    /// The placeholder resolver provider.
    /// </returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (_configuration != null)
        {
            var configurationView = new ConfigurationView(_configuration.Providers.ToArray());
            return new PlaceholderResolverProvider(configurationView, LoggerFactory);
        }

        List<IConfigurationProvider> providers = [];

        if (Sources != null)
        {
            foreach (IConfigurationSource source in Sources)
            {
                IConfigurationProvider provider = source.Build(builder);
                providers.Add(provider);
            }
        }

        return new PlaceholderResolverProvider(providers, LoggerFactory);
    }
}
