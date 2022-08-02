// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka;

public class EurekaServiceInstance : IServiceInstance
{
    private readonly InstanceInfo _info;

    public bool IsSecure => _info.IsSecurePortEnabled;

    public IDictionary<string, string> Metadata => _info.Metadata;

    public int Port => IsSecure ? _info.SecurePort : _info.Port;

    public string ServiceId => _info.AppName;

    public Uri Uri
    {
        get
        {
            string scheme = IsSecure ? "https" : "http";
            return new Uri($"{scheme}://{GetHost()}:{Port}");
        }
    }

    public string Host => GetHost();

    public string InstanceId => _info.InstanceId;

    public EurekaServiceInstance(InstanceInfo info)
    {
        _info = info;
    }

    public string GetHost()
    {
        return _info.HostName;
    }
}
