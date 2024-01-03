// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
#if NET6_0
using System.Reflection;
#endif

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
        ArgumentGuard.NotNull(configuration);

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
                var nextProvider = FindConfigurationProvider<TProvider>(chained);

                if (nextProvider != null)
                {
                    return nextProvider;
                }
            }
        }

        return null;
    }

    private static TProvider? FindConfigurationProvider<TProvider>(ChainedConfigurationProvider provider)
        where TProvider : class, IConfigurationProvider
    {
#if NET6_0
        FieldInfo? field = typeof(ChainedConfigurationProvider).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field != null)
        {
            var configuration = (IConfiguration?)field.GetValue(provider);

            if (configuration != null)
            {
                return FindConfigurationProvider<TProvider>(configuration);
            }
        }

        return null;
#else
        return FindConfigurationProvider<TProvider>(provider.Configuration);
#endif
    }
}
