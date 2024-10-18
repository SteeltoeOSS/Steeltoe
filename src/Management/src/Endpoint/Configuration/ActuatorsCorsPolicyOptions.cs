// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Stores callbacks, which are applied when configuring the Cross-Origin Resource Sharing (CORS) policy for actuator endpoints.
/// </summary>
internal sealed class ActuatorsCorsPolicyOptions
{
    public const string PolicyName = "ActuatorsCorsPolicy";

    public IList<Action<CorsPolicyBuilder>> ConfigureActions { get; } = new List<Action<CorsPolicyBuilder>>();
}
