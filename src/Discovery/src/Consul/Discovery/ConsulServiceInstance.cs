// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A Consul service instance constructed from a ServiceEntry.
/// </summary>
public class ConsulServiceInstance : IServiceInstance
{
    /// <inheritdoc />
    public string ServiceId { get; }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public bool IsSecure { get; }

    /// <inheritdoc />
    public Uri Uri { get; }

    public string[] Tags { get; }

    /// <inheritdoc />
    public IDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceInstance" /> class.
    /// </summary>
    /// <param name="serviceEntry">
    /// the service entry from the Consul server.
    /// </param>
    public ConsulServiceInstance(ServiceEntry serviceEntry)
    {
        Host = ConsulServerUtils.FindHost(serviceEntry);
        Tags = serviceEntry.Service.Tags;
        Metadata = serviceEntry.Service.Meta;
        IsSecure = GetIsSecure(serviceEntry);
        ServiceId = serviceEntry.Service.Service;
        Port = serviceEntry.Service.Port;
        string scheme = IsSecure ? "https" : "http";
        Uri = new Uri($"{scheme}://{Host}:{Port}");
    }

    private static bool GetIsSecure(ServiceEntry serviceEntry)
    {
        if (serviceEntry.Service.Meta == null)
        {
            return false;
        }

        return serviceEntry
            .Service
            .Meta
            .TryGetValue("secure", out string secureString) && bool.Parse(secureString);
    }
}
