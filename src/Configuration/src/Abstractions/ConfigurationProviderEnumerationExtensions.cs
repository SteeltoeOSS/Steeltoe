// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal static class ConfigurationProviderEnumerationExtensions
{
    private static readonly Predicate<IConfigurationProvider> MatchAlways = _ => true;

    /// <summary>
    /// Enumerates all configuration providers, scanning through any <see cref="CompositeConfigurationProvider" /> and
    /// <see cref="ChainedConfigurationProvider" /> providers.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to search in.
    /// </param>
    public static IEnumerable<IConfigurationProvider> EnumerateProviders(this IConfiguration configuration)
    {
        return EnumerateProviders(configuration, MatchAlways);
    }

    /// <summary>
    /// Enumerates all configuration providers of the specified type, scanning through any <see cref="CompositeConfigurationProvider" /> and
    /// <see cref="ChainedConfigurationProvider" /> providers.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The type of configuration provider to enumerate.
    /// </typeparam>
    /// <param name="configuration">
    /// The configuration to search in.
    /// </param>
    public static IEnumerable<TProvider> EnumerateProviders<TProvider>(this IConfiguration configuration)
        where TProvider : IConfigurationProvider
    {
        return EnumerateProviders(configuration, MatchAlways).OfType<TProvider>();
    }

    /// <summary>
    /// Enumerates all configuration providers that match the specified predicate, scanning through any <see cref="CompositeConfigurationProvider" /> and
    /// <see cref="ChainedConfigurationProvider" /> providers.
    /// </summary>
    /// <param name="configuration">
    /// The configuration to search in.
    /// </param>
    /// <param name="predicate">
    /// The provider filter condition.
    /// </param>
    public static IEnumerable<IConfigurationProvider> EnumerateProviders(this IConfiguration configuration, Predicate<IConfigurationProvider> predicate)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(predicate);

        if (configuration is IConfigurationRoot root)
        {
            return FilterProviders(root.Providers, predicate);
        }

        return Array.Empty<IConfigurationProvider>();
    }

    private static IEnumerable<IConfigurationProvider> FilterProviders(IEnumerable<IConfigurationProvider> providers,
        Predicate<IConfigurationProvider> predicate)
    {
        foreach (IConfigurationProvider provider in providers)
        {
            if (provider is ChainedConfigurationProvider chainedConfigurationProvider)
            {
                foreach (IConfigurationProvider match in EnumerateProviders(chainedConfigurationProvider.Configuration, predicate))
                {
                    yield return match;
                }
            }

            if (provider is CompositeConfigurationProvider { ConfigurationRoot: not null } compositeConfigurationProvider)
            {
                foreach (IConfigurationProvider match in EnumerateProviders(compositeConfigurationProvider.ConfigurationRoot, predicate))
                {
                    yield return match;
                }
            }

            if (predicate(provider))
            {
                yield return provider;
            }
        }
    }
}
