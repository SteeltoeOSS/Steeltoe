// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry;

public sealed class CloudFoundryServicesOptions
{
    // See https://docs.cloudfoundry.org/devguide/deploy-apps/environment-variable.html#VCAP-SERVICES

    /// <summary>
    /// Gets the list of Cloud Foundry services from the VCAP_SERVICES environment variable.
    /// </summary>
    public IDictionary<string, IList<CloudFoundryService>> Services { get; } = new Dictionary<string, IList<CloudFoundryService>>();

    /// <summary>
    /// Retrieves a flattened list of all services for all types.
    /// </summary>
    /// <returns>
    /// The complete list of services known to the application.
    /// </returns>
    public IList<CloudFoundryService> GetAllServices()
    {
        return Services.SelectMany(service => service.Value).ToList();
    }

    /// <summary>
    /// Retrieves a list of all services of a given service type.
    /// </summary>
    /// <param name="serviceType">
    /// The type to find services for. May be platform/broker/version dependent. Sample values include: <code>p-mysql</code>, <code>azure-mysql-5-7</code>,
    /// <code>p-configserver</code>, <code>p.configserver</code>.
    /// </param>
    /// <returns>
    /// A list of services configured under the given type.
    /// </returns>
    public IList<CloudFoundryService> GetServicesOfType(string serviceType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceType);

        return Services.TryGetValue(serviceType, out IList<CloudFoundryService>? services) ? services : Array.Empty<CloudFoundryService>();
    }
}
