// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Discovery;

public interface IDiscoveryClient : IServiceInstanceProvider
{
    /// <summary>
    /// Gets the local service instance with information used to register the local service.
    /// </summary>
    /// <returns>
    /// The service instance.
    /// </returns>
    IServiceInstance GetLocalServiceInstance();

    Task ShutdownAsync(CancellationToken cancellationToken);
}
