// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

public class EurekaDiscoveryClient : DiscoveryClient, IDiscoveryClient
{
    private readonly IOptionsMonitor<EurekaClientOptions> _configOptions;
    private readonly IServiceInstance _thisInstance;

    public override IEurekaClientConfiguration ClientConfiguration => _configOptions.CurrentValue;

    public IList<string> Services => GetRegisteredServices();

    public string Description => "Spring Cloud Eureka Client";

    public EurekaDiscoveryClient(IOptionsMonitor<EurekaClientOptions> clientConfig, IOptionsMonitor<EurekaInstanceOptions> instConfig,
        EurekaApplicationInfoManager appInfoManager, IEurekaHttpClient httpClient = null, ILoggerFactory logFactory = null,
        IHttpClientHandlerProvider handlerProvider = null, HttpClient netHttpClient = null)
        : base(appInfoManager, logFactory)
    {
        _thisInstance = new ThisServiceInstance(instConfig);
        _configOptions = clientConfig;
        this.httpClient = httpClient ?? new EurekaHttpClientInternal(clientConfig, logFactory, handlerProvider, netHttpClient);

        Initialize();
    }

    public IList<string> GetRegisteredServices()
    {
        Applications applications = Applications;

        if (applications == null)
        {
            return new List<string>();
        }

        IList<Application> registered = applications.GetRegisteredApplications();
        var names = new List<string>();

        foreach (Application app in registered)
        {
            if (app.Instances.Count == 0)
            {
                continue;
            }

#pragma warning disable S4040 // Strings should be normalized to uppercase
            names.Add(app.Name.ToLowerInvariant());
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return names;
    }

    public IList<IServiceInstance> GetInstances(string serviceId)
    {
        IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
        var instances = new List<IServiceInstance>();

        foreach (InstanceInfo info in infos)
        {
            logger?.LogDebug($"GetInstances returning: {info}");
            instances.Add(new EurekaServiceInstance(info));
        }

        return instances;
    }

    public IServiceInstance GetLocalServiceInstance()
    {
        return _thisInstance;
    }

    public override Task ShutdownAsync()
    {
        appInfoManager.InstanceStatus = InstanceStatus.Down;
        return base.ShutdownAsync();
    }

    private sealed class EurekaHttpClientInternal : EurekaHttpClient
    {
        private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitorOptions;

        protected override IEurekaClientConfiguration Configuration => _optionsMonitorOptions.CurrentValue;

        public EurekaHttpClientInternal(IOptionsMonitor<EurekaClientOptions> optionsMonitor, ILoggerFactory logFactory = null,
            IHttpClientHandlerProvider handlerProvider = null, HttpClient httpClient = null)
        {
            ArgumentGuard.NotNull(optionsMonitor);

            configuration = null;
            _optionsMonitorOptions = optionsMonitor;
            this.handlerProvider = handlerProvider;
            Initialize(new Dictionary<string, string>(), logFactory);
            this.httpClient = httpClient;
        }
    }
}
