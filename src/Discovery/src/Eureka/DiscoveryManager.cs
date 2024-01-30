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

    public virtual EurekaClientOptions ClientOptions { get; protected internal set; }

    public virtual EurekaInstanceOptions InstanceOptions { get; protected internal set; }

    protected DiscoveryManager()
    {
    }

    public virtual void Initialize(EurekaClientOptions clientOptions, ILoggerFactory loggerFactory = null)
    {
        Initialize(clientOptions, (EurekaHttpClient)null, loggerFactory);
    }

    public virtual void Initialize(EurekaClientOptions clientOptions, EurekaInstanceOptions instanceOptions, ILoggerFactory loggerFactory = null)
    {
        Initialize(clientOptions, instanceOptions, null, loggerFactory);
    }

    public virtual void Initialize(EurekaClientOptions clientOptions, EurekaHttpClient httpClient, ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientOptions);

        logger = loggerFactory?.CreateLogger<DiscoveryManager>();
        ClientOptions = clientOptions;
        Client = new DiscoveryClient(clientOptions, httpClient, loggerFactory);
    }

    public virtual void Initialize(EurekaClientOptions clientOptions, EurekaInstanceOptions instanceOptions, EurekaHttpClient httpClient,
        ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(clientOptions);
        ArgumentGuard.NotNull(instanceOptions);

        logger = loggerFactory?.CreateLogger<DiscoveryManager>();
        ClientOptions = clientOptions;
        InstanceOptions = instanceOptions;

        if (ApplicationInfoManager.Instance.InstanceInfo == null)
        {
            ApplicationInfoManager.Instance.Initialize(instanceOptions, loggerFactory);
        }

        Client = new DiscoveryClient(clientOptions, httpClient, loggerFactory);
    }
}
