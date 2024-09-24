// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Steeltoe.Common.Json;

public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Registers a contract modifier in the specified <see cref="JsonSerializerOptions" /> that skips serialization of empty collections marked with
    /// <see cref="JsonIgnoreEmptyCollectionAttribute" />.
    /// </summary>
    /// <param name="options">
    /// The JSON serializer options to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="options" /> so that additional calls can be chained.
    /// </returns>
    public static JsonSerializerOptions AddJsonIgnoreEmptyCollection(this JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                SkipEmptyCollection
            }
        });

        return options;
    }

    private static void SkipEmptyCollection(JsonTypeInfo info)
    {
        foreach (JsonPropertyInfo property in info.Properties)
        {
            if (property.AttributeProvider != null && property.AttributeProvider.IsDefined(typeof(JsonIgnoreEmptyCollectionAttribute), true))
            {
                property.ShouldSerialize = (_, value) =>
                {
                    var enumerable = value as IEnumerable;
                    return enumerable == null || enumerable.Cast<object>().Any();
                };
            }
        }
    }
}
