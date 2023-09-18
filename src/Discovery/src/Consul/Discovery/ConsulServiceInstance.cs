// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// A Consul service instance constructed from a ServiceEntry
/// </summary>
public class ConsulServiceInstance : IServiceInstance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceInstance"/> class.
    /// </summary>
    /// <param name="serviceEntry">the service entry from the Consul server</param>
    public ConsulServiceInstance(ServiceEntry serviceEntry)
    {
        // TODO: 3.0  ID = healthService.ID;
        Host = ConsulServerUtils.FindHost(serviceEntry);

        if (serviceEntry.Service.Meta == null)
        {
            var metadata = ConsulServerUtils.GetMetadata(serviceEntry);
            Metadata = metadata;
            IsSecure = GetIsSecure(metadata);
        }
        else
        {
            Metadata = serviceEntry.Service.Meta;
            IsSecure = GetIsSecure(serviceEntry.Service.Meta);
            Tags = serviceEntry.Service.Tags;
        }

        ServiceId = serviceEntry.Service.Service;
        Port = serviceEntry.Service.Port;
        var scheme = IsSecure ? "https" : "http";
        Uri = new Uri($"{scheme}://{Host}:{Port}");
    }

    #region Implementation of IServiceInstance

    /// <inheritdoc/>
    public string ServiceId { get; }

    /// <inheritdoc/>
    public string Host { get; }

    /// <inheritdoc/>
    public int Port { get; }

    /// <inheritdoc/>
    public bool IsSecure { get; }

    /// <inheritdoc/>
    public Uri Uri { get; }

    /// <inheritdoc/>
    public IDictionary<string, string> Metadata { get; }

    #endregion Implementation of IServiceInstance

    public string[] Tags { get; }

    private static bool GetIsSecure(IDictionary<string, string> meta)
    {
        if (meta == null)
        {
            return false;
        }

        return meta.TryGetValue("secure", out string secureString) && bool.Parse(secureString);
    }
}