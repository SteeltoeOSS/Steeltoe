// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Consul;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// A service instance in Consul, either returned from a Consul server or representing the currently running application.
/// </summary>
internal sealed class ConsulServiceInstance : IServiceInstance
{
    private static readonly IReadOnlyList<string> EmptyStringList = Array.Empty<string>().AsReadOnly();
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

    private ConsulServiceInstance(string serviceId, string instanceId, string host, Uri uri, IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, string?> metadata)
    {
        ArgumentNullException.ThrowIfNull(serviceId);
        ArgumentNullException.ThrowIfNull(instanceId);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(uri);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(metadata);

        ServiceId = serviceId;
        InstanceId = instanceId;
        Host = host;
        Port = uri.Port;
        IsSecure = uri.Scheme == Uri.UriSchemeHttps;
        Uri = uri;
        NonSecureUri = IsSecure ? null : uri;
        SecureUri = IsSecure ? uri : null;
        Tags = tags;
        Metadata = metadata;
    }

    /// <summary>
    /// Creates a service instance from a service entry returned by the Consul server.
    /// </summary>
    /// <param name="serviceEntry">
    /// The service entry from the Consul server.
    /// </param>
    internal static ConsulServiceInstance FromServiceEntry(ServiceEntry serviceEntry)
    {
        ArgumentNullException.ThrowIfNull(serviceEntry);

        string host = ConsulServerUtils.FindHost(serviceEntry);
        IReadOnlyDictionary<string, string?> metadata = serviceEntry.Service.Meta?.AsReadOnly() ?? EmptyStringDictionary;
        bool isSecure = metadata.TryGetValue("secure", out string? secureString) && bool.TryParse(secureString, out bool isSecureValue) && isSecureValue;
        string scheme = isSecure ? "https" : "http";
        Uri uri = CreateUri(scheme, host, serviceEntry.Service.Port);
        IReadOnlyList<string> tags = serviceEntry.Service.Tags?.ToArray().AsReadOnly() ?? EmptyStringList;

        return new ConsulServiceInstance(serviceEntry.Service.Service, serviceEntry.Service.ID, host, uri, tags, metadata);
    }

    /// <summary>
    /// Creates the service instance for the currently running application, as a snapshot of the specified options.
    /// </summary>
    /// <param name="options">
    /// The discovery options to take values from.
    /// </param>
    internal static ConsulServiceInstance FromOptions(ConsulDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string hostName = options.HostName!;
        int port = options.Port > 0 ? options.Port : 0;
        Uri uri = CreateUri(options.EffectiveScheme, hostName, port);
        ReadOnlyCollection<string> tags = options.Tags.ToArray().AsReadOnly();
        bool isSecure = uri.Scheme == Uri.UriSchemeHttps;
        IReadOnlyDictionary<string, string?> metadata = CreateMetadata(options, isSecure);

        return new ConsulServiceInstance(options.ServiceName!, options.InstanceId!, hostName, uri, tags, metadata);
    }

    private static Uri CreateUri(string scheme, string host, int port)
    {
        if (Uri.TryCreate($"{scheme}://{host}:{port}", UriKind.Absolute, out Uri? uri))
        {
            return uri;
        }

        throw new UriFormatException($"Failed to build URI from components. Scheme={scheme}, Host={host},Port={port}.");
    }

    private static ReadOnlyDictionary<string, string?> CreateMetadata(ConsulDiscoveryOptions options, bool isSecure)
    {
        Dictionary<string, string?> metadata = options.Metadata.ToDictionary(pair => pair.Key, string? (pair) => pair.Value);

        if (!string.IsNullOrEmpty(options.InstanceZone) && !string.IsNullOrEmpty(options.DefaultZoneMetadataName))
        {
            metadata.TryAdd(options.DefaultZoneMetadataName, options.InstanceZone);
        }

        if (!string.IsNullOrEmpty(options.InstanceGroup))
        {
            metadata.TryAdd("group", options.InstanceGroup);
        }

        // Store the secure flag in the metadata, so that clients will be able to figure out whether to use http or https automatically.
        metadata["secure"] = isSecure ? "true" : "false";

        return metadata.AsReadOnly();
    }
}
