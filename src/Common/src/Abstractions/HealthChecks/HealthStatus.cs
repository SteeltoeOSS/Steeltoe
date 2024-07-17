// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.CasingConventions;

namespace Steeltoe.Common.HealthChecks;

[JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
public enum HealthStatus
{
    Unknown,
    Up,
    Warning,
    OutOfService,
    Down
}
