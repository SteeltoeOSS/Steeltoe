// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonApplicationsRoot))]
[JsonSerializable(typeof(List<JsonApplication?>))]
[JsonSerializable(typeof(JsonInstanceInfoRoot))]
[JsonSerializable(typeof(List<JsonInstanceInfo?>))]
internal sealed partial class EurekaJsonSerializerContext : JsonSerializerContext;
