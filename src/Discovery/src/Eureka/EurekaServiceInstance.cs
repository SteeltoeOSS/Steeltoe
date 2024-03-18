// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

internal sealed class EurekaServiceInstance : IServiceInstance
{
    private readonly InstanceInfo _instance;

    public string ServiceId => _instance.AppName;
    public string Host => _instance.HostName;
    public int Port => IsSecure ? _instance.SecurePort : _instance.NonSecurePort;
    public bool IsSecure => _instance.IsSecurePortEnabled;
    public Uri Uri => GetUri();
    public IReadOnlyDictionary<string, string?> Metadata => _instance.Metadata;

    public EurekaServiceInstance(InstanceInfo instance)
    {
        ArgumentGuard.NotNull(instance);

        _instance = instance;
    }

    private Uri GetUri()
    {
        string scheme = IsSecure ? "https" : "http";
        return new Uri($"{scheme}://{Host}:{Port}");
    }
}
