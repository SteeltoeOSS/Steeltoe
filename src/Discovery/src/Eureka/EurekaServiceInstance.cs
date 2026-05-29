// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

/// <summary>
/// Represents an application instance in Eureka.
/// </summary>
internal sealed class EurekaServiceInstance : IServiceInstance
{
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

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> Metadata { get; }

    public EurekaServiceInstance(InstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ServiceId = instance.AppName;
        InstanceId = instance.InstanceId;
        Host = instance.HostName;
        Metadata = instance.Metadata;

        if (instance is { IsNonSecurePortEnabled: true, NonSecurePort: > 0 })
        {
#pragma warning disable S5332 // Using clear-text protocols is security-sensitive
            NonSecureUri = new Uri($"http://{Host}:{instance.NonSecurePort}");
#pragma warning restore S5332 // Using clear-text protocols is security-sensitive
            Port = instance.NonSecurePort;
        }

        if (instance is { IsSecurePortEnabled: true, SecurePort: > 0 })
        {
            SecureUri = new Uri($"https://{Host}:{instance.SecurePort}");
            Port = instance.SecurePort;
        }

        IsSecure = instance.IsSecurePortEnabled;
        Uri = new Uri($"{(IsSecure ? "https" : "http")}://{Host}:{Port}");
    }
}
