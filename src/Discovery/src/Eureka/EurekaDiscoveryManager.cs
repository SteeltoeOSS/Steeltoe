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

    public static EurekaDiscoveryManager SharedInstance { get; } = new();
    public DiscoveryClient? Client { get; private set; }

    public EurekaClientOptions? ClientOptions
    {
        get => _clientOptionsMonitor?.CurrentValue;
        private set
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
        private set
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

    internal void Initialize(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        _clientOptionsMonitor = clientOptionsMonitor;
        Client = new DiscoveryClient(clientOptionsMonitor, null, loggerFactory);
    }

    internal void Initialize(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(loggerFactory);

        _clientOptionsMonitor = clientOptionsMonitor;
        _instanceOptionsMonitor = instanceOptionsMonitor;

        if (EurekaApplicationInfoManager.SharedInstance.InstanceInfo == null)
        {
            ILogger<EurekaApplicationInfoManager> logger = loggerFactory.CreateLogger<EurekaApplicationInfoManager>();
            EurekaApplicationInfoManager.SharedInstance.Initialize(instanceOptionsMonitor, logger);
        }

        Client = new DiscoveryClient(clientOptionsMonitor, null, loggerFactory);
    }

    internal static void ResetSharedInstance()
    {
        SharedInstance.ClientOptions = null;
        SharedInstance.Client = null;
        SharedInstance.InstanceOptions = null;

    }
}
