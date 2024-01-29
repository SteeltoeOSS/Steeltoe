// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryManager : DiscoveryManager
{
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly IOptionsMonitor<EurekaInstanceOptions> _instanceOptionsMonitor;

    public override EurekaClientOptions ClientOptions => _clientOptionsMonitor.CurrentValue;
    public override EurekaInstanceOptions InstanceOptions => _instanceOptionsMonitor.CurrentValue;

    // Constructor used via Dependency Injection
    public EurekaDiscoveryManager(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        EurekaDiscoveryClient client)
    {
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(clientOptionsMonitor);

        _clientOptionsMonitor = clientOptionsMonitor;
        _instanceOptionsMonitor = instanceOptionsMonitor;
        Client = client;
    }
}
