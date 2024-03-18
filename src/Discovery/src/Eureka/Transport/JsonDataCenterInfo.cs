// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonDataCenterInfo
{
    [JsonPropertyName("@class")]
    public string? ClassName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    public static JsonDataCenterInfo Create(string? className, string? name)
    {
        return new JsonDataCenterInfo
        {
            ClassName = className,
            Name = name
        };
    }
}
