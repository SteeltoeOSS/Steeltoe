// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Eureka;

public class ThisServiceInstance : IServiceInstance
{
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instConfig;

    private EurekaInstanceOptions InstConfig
    {
        get
        {
            return _instConfig.CurrentValue;
        }
    }

    public ThisServiceInstance(IOptionsMonitor<EurekaInstanceOptions> instConfig)
    {
        _instConfig = instConfig;
    }

    public string GetHost()
    {
        return InstConfig.GetHostName(false);
    }

    public bool IsSecure => InstConfig.SecurePortEnabled;

    public IDictionary<string, string> Metadata => InstConfig.MetadataMap;

    public int Port => InstConfig.NonSecurePort == -1 ? EurekaInstanceConfig.Default_NonSecurePort : InstConfig.NonSecurePort;

    public int SecurePort => InstConfig.SecurePort == -1 ? EurekaInstanceConfig.Default_SecurePort : InstConfig.SecurePort;

    public string ServiceId => InstConfig.AppName;

    public Uri Uri
    {
        get
        {
            var scheme = IsSecure ? "https" : "http";
            var uriPort = IsSecure ? SecurePort : Port;
            var uri = new Uri($"{scheme}://{GetHost()}:{uriPort}");
            return uri;
        }
    }

    public string Host => GetHost();
}
