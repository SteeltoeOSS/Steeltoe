// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using System.Threading.Tasks;

namespace Steeltoe.Discovery;

public interface IDiscoveryClient : IServiceInstanceProvider
{
    /// <summary>
    ///  ServiceInstance with information used to register the local service
    /// </summary>
    /// <returns>The IServiceInstance</returns>
    IServiceInstance GetLocalServiceInstance();

    Task ShutdownAsync();
}
