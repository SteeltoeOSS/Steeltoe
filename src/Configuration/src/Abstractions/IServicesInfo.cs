// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration;

public interface IServicesInfo
{
    /// <summary>
    /// Retrieves a list of all service instances for all service types.
    /// </summary>
    /// <returns>
    /// A complete list of service instances known to the application.
    /// </returns>
    IEnumerable<Service> GetServicesList();

    /// <summary>
    /// Retrieves a list of all service instances of a given service type.
    /// </summary>
    /// <param name="serviceType">
    /// String value that identifies the service type. May be platform/broker/version dependent.
    /// </param>
    /// <remarks>
    /// Sample values include: p-mysql, azure-mysql-5-7, p-configserver, p.configserver.
    /// </remarks>
    /// <returns>
    /// A list of service instances configured under the given type.
    /// </returns>
    IEnumerable<Service> GetInstancesOfType(string serviceType);
}
