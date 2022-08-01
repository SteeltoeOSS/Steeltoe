// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Steeltoe.Extensions.Configuration.Placeholder;

/// <summary>
/// Configuration source used in creating a <see cref="PlaceholderResolverProvider"/> that resolves placeholders
/// A placeholder takes the form of: <code> ${some:config:reference?default_if_not_present}></code>
/// </summary>
public class PlaceholderResolverSource : IConfigurationSource
{
    internal IConfigurationRoot Configuration;
    internal ConfigurationView ConfigurationView;
    internal ILoggerFactory LoggerFactory;

    internal IList<IConfigurationSource> Sources;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderResolverSource"/> class.
    /// </summary>
    /// <param name="sources">the configuration sources to use.</param>
    /// <param name="logFactory">the logger factory to use.</param>
    public PlaceholderResolverSource(IList<IConfigurationSource> sources, ILoggerFactory logFactory = null)
    {
        if (sources == null)
        {
            throw new ArgumentNullException(nameof(sources));
        }

        this.Sources = new List<IConfigurationSource>(sources);
        LoggerFactory = logFactory ?? new NullLoggerFactory();
    }

    public PlaceholderResolverSource(IConfigurationRoot configuration, ILoggerFactory loggerFactory = null)
    {
        this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.LoggerFactory = loggerFactory ?? new NullLoggerFactory();
    }

    /// <summary>
    /// Builds a <see cref="PlaceholderResolverProvider"/> from the sources.
    /// </summary>
    /// <param name="builder">the provided builder.</param>
    /// <returns>the placeholder resolver provider.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (Configuration != null)
        {
            return new PlaceholderResolverProvider(new ConfigurationView(Configuration.Providers.ToList()), LoggerFactory);
        }

        var providers = new List<IConfigurationProvider>();
        foreach (var source in Sources)
        {
            var provider = source.Build(builder);
            providers.Add(provider);
        }

        return new PlaceholderResolverProvider(providers, LoggerFactory);
    }
}
