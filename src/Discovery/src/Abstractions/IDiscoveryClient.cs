// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery;

public interface IDiscoveryClient : IServiceInstanceProvider
{
    /// <summary>
    /// Gets the local service instance with information used to register the local service.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The service instance.
    /// </returns>
    Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken);

    Task ShutdownAsync(CancellationToken cancellationToken);
}
