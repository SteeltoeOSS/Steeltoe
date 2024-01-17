// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

public class DiscoveryManager
{
    protected static readonly DiscoveryManager InnerInstance = new();

    protected ILogger logger;

    public static DiscoveryManager Instance => InnerInstance;

    public virtual DiscoveryClient Client { get; protected internal set; }

    public virtual EurekaClientConfiguration ClientConfiguration { get; protected internal set; }

    public virtual EurekaInstanceConfiguration InstanceConfig { get; protected internal set; }

    protected DiscoveryManager()
    {
    }

    public virtual void Initialize(EurekaClientConfiguration clientConfiguration, ILoggerFactory loggerFactory = null)
    {
        Initialize(clientConfiguration, (EurekaHttpClient)null, loggerFactory);
    }

    public virtual void Initialize(EurekaClientConfiguration clientConfiguration, EurekaInstanceConfiguration instanceConfig, ILoggerFactory loggerFactory = null)
    {
        Initialize(clientConfiguration, instanceConfig, null, loggerFactory);
    }

    public virtual void Initialize(EurekaClientConfiguration clientConfiguration, EurekaHttpClient httpClient, ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientConfiguration);

        logger = loggerFactory?.CreateLogger<DiscoveryManager>();
        ClientConfiguration = clientConfiguration;
        Client = new DiscoveryClient(clientConfiguration, httpClient, loggerFactory);
    }

    public virtual void Initialize(EurekaClientConfiguration clientConfiguration, EurekaInstanceConfiguration instanceConfig, EurekaHttpClient httpClient,
        ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientConfiguration);
        ArgumentGuard.NotNull(instanceConfig);

        logger = loggerFactory?.CreateLogger<DiscoveryManager>();
        ClientConfiguration = clientConfiguration;
        InstanceConfig = instanceConfig;

        if (ApplicationInfoManager.Instance.InstanceInfo == null)
        {
            ApplicationInfoManager.Instance.Initialize(instanceConfig, loggerFactory);
        }

        Client = new DiscoveryClient(clientConfiguration, httpClient, loggerFactory);
    }
}
