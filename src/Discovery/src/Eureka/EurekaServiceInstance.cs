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
    public string ServiceId { get; }
    public string InstanceId { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsSecure { get; }
    public Uri Uri { get; }
    public IReadOnlyDictionary<string, string?> Metadata { get; }

    public EurekaServiceInstance(InstanceInfo instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ServiceId = instance.AppName;
        InstanceId = instance.InstanceId;
        Host = instance.HostName;
        Port = GetPort(instance);
        IsSecure = instance.IsSecurePortEnabled;
        Uri = new Uri($"{(IsSecure ? "https" : "http")}://{Host}:{Port}");
        Metadata = instance.Metadata;
    }

    private static int GetPort(InstanceInfo instance)
    {
        if (instance.IsSecurePortEnabled)
        {
            return instance.SecurePort;
        }

        if (instance.IsNonSecurePortEnabled)
        {
            return instance.NonSecurePort;
        }

        return 0;
    }
}
