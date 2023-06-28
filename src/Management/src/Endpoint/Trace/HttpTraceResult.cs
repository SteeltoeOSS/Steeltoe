// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Trace;

[JsonDerivedType(typeof(HttpTracesV1), typeDiscriminator: "V1")]
[JsonDerivedType(typeof(HttpTracesV2), typeDiscriminator: "V2")]
public class HttpTraceResult
{
    internal MediaTypeVersion CurrentVersion { get; set; }
}
