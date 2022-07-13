// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steeltoe.Common.Util;

/// <summary>
/// Converts between pascal-cased enum members and their snake-case-all-caps string representation.
/// <example>
/// <code><![CDATA[
/// HealthStatus.OutOfService <-> OUT_OF_SERVICE
/// ]]></code>
/// </example>
/// </summary>
public sealed class SnakeCaseAllCapsEnumMemberJsonConverter : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(SnakeCaseEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, SnakeCaseStyle.AllCaps);
    }
}
