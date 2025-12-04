// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// A service instance returned from a Consul server.
/// </summary>
internal sealed class ConsulServiceInstance : IServiceInstance
{
    private static readonly IReadOnlyList<string> EmptyStringList = Array.Empty<string>();
    private static readonly IReadOnlyDictionary<string, string?> EmptyStringDictionary = new Dictionary<string, string?>().AsReadOnly();

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <inheritdoc />
    public string InstanceId { get; }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public bool IsSecure { get; }

    /// <inheritdoc />
    public Uri Uri { get; }

    /// <inheritdoc />
    public Uri? NonSecureUri { get; }

    /// <inheritdoc />
    public Uri? SecureUri { get; }

    public IReadOnlyList<string> Tags { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceInstance" /> class.
    /// </summary>
    /// <param name="serviceEntry">
    /// The service entry from the Consul server.
    /// </param>
    internal ConsulServiceInstance(ServiceEntry serviceEntry)
    {
        ArgumentNullException.ThrowIfNull(serviceEntry);

        Host = ConsulServerUtils.FindHost(serviceEntry);
        Tags = serviceEntry.Service.Tags ?? EmptyStringList;
        Metadata = serviceEntry.Service.Meta?.AsReadOnly() ?? EmptyStringDictionary;
        IsSecure = Metadata.TryGetValue("secure", out string? secureString) && secureString != null && bool.Parse(secureString);
        ServiceId = serviceEntry.Service.Service;
        InstanceId = serviceEntry.Service.ID;
        Port = serviceEntry.Service.Port;
        Uri = new Uri($"{(IsSecure ? "https" : "http")}://{Host}:{Port}");
        NonSecureUri = IsSecure ? null : Uri;
        SecureUri = IsSecure ? Uri : null;
    }
}
