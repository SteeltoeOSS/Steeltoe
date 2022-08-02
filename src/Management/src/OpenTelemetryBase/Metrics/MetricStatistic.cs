// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// ReSharper disable IdentifierTypo

using System.Text.Json.Serialization;
using Steeltoe.Common.Util;

namespace Steeltoe.Management.OpenTelemetry.Metrics;

[JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
public enum MetricStatistic
{
    Total,
    TotalTime,
    Count,
    Max,
    Value,
    Unknown,
    Activetasks,
    Duration
}
