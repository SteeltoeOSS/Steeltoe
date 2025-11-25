// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Discovery;

public interface IServiceInstance
{
    /// <summary>
    /// Gets the service ID as registered by the discovery client.
    /// </summary>
    string ServiceId { get; }

    /// <summary>
    /// Gets the instance ID as registered by the discovery client.
    /// </summary>
    string InstanceId { get; }

    /// <summary>
    /// Gets the hostname of the registered service instance.
    /// </summary>
    string Host { get; }

    /// <summary>
    /// Gets the port of the registered service instance.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Gets a value indicating whether the scheme of the registered service instance is https.
    /// </summary>
    bool IsSecure { get; }

    /// <summary>
    /// Gets the resolved address of the registered service instance.
    /// </summary>
    Uri Uri { get; }

    /// <summary>
    /// Gets the HTTP-based resolved address of the registered service instance, if available.
    /// </summary>
    Uri? NonSecureUri { get; }

    /// <summary>
    /// Gets the HTTPS-based resolved address of the registered service instance, if available.
    /// </summary>
    Uri? SecureUri { get; }

    /// <summary>
    /// Gets the key/value metadata associated with this service instance.
    /// </summary>
    IReadOnlyDictionary<string, string?> Metadata { get; }
}
