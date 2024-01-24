// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A service instance returned from a Consul server.
/// </summary>
public sealed class ConsulServiceInstance : IServiceInstance
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

    public IList<string> Tags { get; }

    /// <inheritdoc />
    public IDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceInstance" /> class.
    /// </summary>
    /// <param name="serviceEntry">
    /// The service entry from the Consul server.
    /// </param>
    public ConsulServiceInstance(ServiceEntry serviceEntry)
    {
        ArgumentGuard.NotNull(serviceEntry);

        Host = ConsulServerUtils.FindHost(serviceEntry);
        Tags = serviceEntry.Service.Tags;
        Metadata = serviceEntry.Service.Meta;
        IsSecure = serviceEntry.Service.Meta != null && serviceEntry.Service.Meta.TryGetValue("secure", out string? secureString) && bool.Parse(secureString);
        ServiceId = serviceEntry.Service.Service;
        Port = serviceEntry.Service.Port;
        string scheme = IsSecure ? "https" : "http";
        Uri = new Uri($"{scheme}://{Host}:{Port}");
    }
}
