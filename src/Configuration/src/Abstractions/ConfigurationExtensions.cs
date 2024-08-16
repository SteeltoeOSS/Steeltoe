// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal static class ConfigurationExtensions
{
    /// <summary>
    /// Finds the first configuration provider of the specified type, scanning through any <see cref="IPlaceholderResolverProvider" /> and
    /// <see cref="ChainedConfigurationProvider" /> composite providers.
    /// </summary>
    /// <typeparam name="TProvider">
    /// The type of <see cref="IConfigurationProvider" /> to find.
    /// </typeparam>
    /// <param name="configuration">
    /// The configuration to search in.
    /// </param>
    public static TProvider? FindConfigurationProvider<TProvider>(this IConfiguration configuration)
        where TProvider : class, IConfigurationProvider
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration is IConfigurationRoot root)
        {
            return FindConfigurationProvider<TProvider>(root.Providers);
        }

        return null;
    }

    private static TProvider? FindConfigurationProvider<TProvider>(IEnumerable<IConfigurationProvider> providers)
        where TProvider : class, IConfigurationProvider
    {
        foreach (IConfigurationProvider provider in providers)
        {
            if (provider is TProvider matchingProvider)
            {
                return matchingProvider;
            }

            if (provider is IPlaceholderResolverProvider placeholder)
            {
                var nextProvider = FindConfigurationProvider<TProvider>(placeholder.Providers);

                if (nextProvider != null)
                {
                    return nextProvider;
                }
            }

            if (provider is ChainedConfigurationProvider chained)
            {
                var nextProvider = FindConfigurationProvider<TProvider>(chained.Configuration);

                if (nextProvider != null)
                {
                    return nextProvider;
                }
            }
        }

        return null;
    }
}
