// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text.Json.Serialization;
using Steeltoe.Common.Http.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonPortWrapper
{
    [JsonPropertyName("@enabled")]
    [JsonConverter(typeof(BoolStringJsonConverter))]
    public bool Enabled { get; set; }

    [JsonPropertyName("$")]
    public int Port { get; set; }

    public static JsonPortWrapper Create(bool enabled, int port)
    {
        return new JsonPortWrapper
        {
            Enabled = enabled,
            Port = port
        };
    }
}
