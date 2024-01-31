// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Discovery.Eureka;

public sealed class EurekaDiscoveryManager
{
    private IOptionsMonitor<EurekaClientOptions>? _clientOptionsMonitor;
    private IOptionsMonitor<EurekaInstanceOptions>? _instanceOptionsMonitor;

    public static EurekaDiscoveryManager Instance { get; } = new();
    public DiscoveryClient? Client { get; internal set; }

    public EurekaClientOptions? ClientOptions
    {
        get => _clientOptionsMonitor?.CurrentValue;
        internal set
        {
            if (value != null)
            {
                throw new NotSupportedException();
            }

            _clientOptionsMonitor = null;
        }
    }

    public EurekaInstanceOptions? InstanceOptions
    {
        get => _instanceOptionsMonitor?.CurrentValue;
        internal set
        {
            if (value != null)
            {
                throw new NotSupportedException();
            }

            _instanceOptionsMonitor = null;
        }
    }

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

    private EurekaDiscoveryManager()
    {
    }

    internal void Initialize(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, ILoggerFactory? loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);

        _clientOptionsMonitor = clientOptionsMonitor;
        Client = new DiscoveryClient(clientOptionsMonitor.CurrentValue, null, loggerFactory);
    }

    internal void Initialize(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);

        _clientOptionsMonitor = clientOptionsMonitor;
        _instanceOptionsMonitor = instanceOptionsMonitor;

        if (ApplicationInfoManager.Instance.InstanceInfo == null)
        {
            ApplicationInfoManager.Instance.Initialize(instanceOptionsMonitor.CurrentValue, loggerFactory);
        }

        Client = new DiscoveryClient(clientOptionsMonitor.CurrentValue, null, loggerFactory);
    }
}
