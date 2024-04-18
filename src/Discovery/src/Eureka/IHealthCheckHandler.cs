// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Used to determine the status of this application instance, before sending it to Eureka.
/// </summary>
public interface IHealthCheckHandler
{
    Task<InstanceStatus> GetStatusAsync(CancellationToken cancellationToken);
}
