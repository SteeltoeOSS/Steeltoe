// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

internal sealed class EurekaServiceInstance : IServiceInstance
{
    private readonly InstanceInfo _info;

    public string ServiceId => _info.AppName;
    public string Host => _info.HostName;
    public int Port => IsSecure ? _info.SecurePort : _info.Port;
    public bool IsSecure => _info.IsSecurePortEnabled;
    public Uri Uri => GetUri();
    public IDictionary<string, string> Metadata => _info.Metadata;

    public EurekaServiceInstance(InstanceInfo info)
    {
        ArgumentGuard.NotNull(info);

        _info = info;
    }

    private Uri GetUri()
    {
        string scheme = IsSecure ? "https" : "http";
        return new Uri($"{scheme}://{Host}:{Port}");
    }
}
