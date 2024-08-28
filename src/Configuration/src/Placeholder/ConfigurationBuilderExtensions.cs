// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.Placeholder;

internal static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Finds the first configuration source of the specified type, scanning through any <see cref="PlaceholderResolverSource" /> composite sources.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of <see cref="IConfigurationSource" /> to find.
    /// </typeparam>
    /// <param name="builder">
    /// The configuration builder to search in.
    /// </param>
    public static TSource? FindConfigurationSource<TSource>(this IConfigurationBuilder builder)
        where TSource : class, IConfigurationSource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return FindConfigurationSource<TSource>(builder.Sources);
    }

    public static TSource? FindConfigurationSource<TSource>(this IEnumerable<IConfigurationSource> sources)
        where TSource : class, IConfigurationSource
    {
        foreach (IConfigurationSource source in sources)
        {
            if (source is TSource matchingSource)
            {
                return matchingSource;
            }

            if (source is PlaceholderResolverSource { Sources: not null } placeholder)
            {
                var nextSource = FindConfigurationSource<TSource>(placeholder.Sources);

                if (nextSource != null)
                {
                    return nextSource;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all configuration sources of the specified type, scanning through any <see cref="PlaceholderResolverSource" /> composite sources.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of <see cref="IConfigurationSource" /> to find.
    /// </typeparam>
    /// <param name="builder">
    /// The configuration builder to search in.
    /// </param>
    public static IEnumerable<TSource> GetConfigurationSources<TSource>(this IConfigurationBuilder builder)
        where TSource : class, IConfigurationSource
    {
        ArgumentNullException.ThrowIfNull(builder);

        List<TSource> sources = [];
        AddConfigurationSources(builder.Sources, sources);
        return sources;
    }

    private static void AddConfigurationSources<TSource>(IEnumerable<IConfigurationSource> sourcesToScan, List<TSource> foundSources)
        where TSource : class, IConfigurationSource
    {
        foreach (IConfigurationSource source in sourcesToScan)
        {
            if (source is TSource matchingSource)
            {
                foundSources.Add(matchingSource);
            }

            if (source is PlaceholderResolverSource { Sources: not null } placeholder)
            {
                AddConfigurationSources(placeholder.Sources, foundSources);
            }
        }
    }
}
