// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal static class ConfigurationSourceEnumerationExtensions
{
    private static readonly Predicate<IConfigurationSource> MatchAlways = _ => true;

    /// <summary>
    /// Enumerates all configuration sources, scanning through any <see cref="ICompositeConfigurationSource" /> sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder to search in.
    /// </param>
    public static IEnumerable<IConfigurationSource> EnumerateSources(this IConfigurationBuilder builder)
    {
        return EnumerateSources(builder, MatchAlways);
    }

    /// <summary>
    /// Enumerates all configuration sources of the specified type, scanning through any <see cref="ICompositeConfigurationSource" /> sources.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of configuration source to enumerate.
    /// </typeparam>
    /// <param name="builder">
    /// The configuration builder to search in.
    /// </param>
    public static IEnumerable<TSource> EnumerateSources<TSource>(this IConfigurationBuilder builder)
        where TSource : IConfigurationSource
    {
        return EnumerateSources(builder, MatchAlways).OfType<TSource>();
    }

    /// <summary>
    /// Enumerates all configuration sources that match the specified predicate, scanning through any <see cref="ICompositeConfigurationSource" /> sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder to search in.
    /// </param>
    /// <param name="predicate">
    /// The source filter condition.
    /// </param>
    public static IEnumerable<IConfigurationSource> EnumerateSources(this IConfigurationBuilder builder, Predicate<IConfigurationSource> predicate)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(predicate);

        return FilterSources(builder.Sources, predicate);
    }

    private static IEnumerable<IConfigurationSource> FilterSources(IEnumerable<IConfigurationSource> sources, Predicate<IConfigurationSource> predicate)
    {
        foreach (IConfigurationSource source in sources)
        {
            if (source is ICompositeConfigurationSource compositeConfigurationSource)
            {
                foreach (IConfigurationSource match in FilterSources(compositeConfigurationSource.Sources, predicate))
                {
                    yield return match;
                }
            }

            if (predicate(source))
            {
                yield return source;
            }
        }
    }
}
