// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryManager : DiscoveryManager
{
    private readonly IOptionsMonitor<EurekaClientOptions> _clientConfig;
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instConfig;

    // Constructor used via Dependency Injection
    public EurekaDiscoveryManager(
        IOptionsMonitor<EurekaClientOptions> clientConfig,
        IOptionsMonitor<EurekaInstanceOptions> instConfig,
        EurekaDiscoveryClient client,
        ILoggerFactory logFactory = null)
    {
        _logger = logFactory?.CreateLogger<DiscoveryManager>();
        _clientConfig = clientConfig;
        _instConfig = instConfig;
        Client = client;
    }

    public override IEurekaClientConfig ClientConfig
    {
        get
        {
            return _clientConfig.CurrentValue;
        }
    }

    public override IEurekaInstanceConfig InstanceConfig
    {
        get
        {
            return _instConfig.CurrentValue;
        }
    }
}