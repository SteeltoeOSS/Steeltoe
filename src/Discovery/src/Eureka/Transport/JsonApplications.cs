// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http.Serialization;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

internal sealed class JsonApplications
{
    [JsonPropertyName("apps__hashcode")]
    public string AppsHashCode { get; set; }

    [JsonPropertyName("versions__delta")]
    [JsonConverter(typeof(LongStringJsonConverter))]
    public long VersionDelta { get; set; }

    [JsonPropertyName("application")]
    [JsonConverter(typeof(JsonApplicationConverter))]
    public IList<JsonApplication> Applications { get; set; }
}
