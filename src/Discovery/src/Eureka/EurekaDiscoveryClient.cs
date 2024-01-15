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

    public override EurekaClientConfiguration ClientConfiguration => _configOptions.CurrentValue;

    public string Description => "Spring Cloud Eureka Client";

    public EurekaDiscoveryClient(IOptionsMonitor<EurekaClientOptions> clientConfig, IOptionsMonitor<EurekaInstanceOptions> instConfig,
        EurekaApplicationInfoManager appInfoManager, EurekaHttpClient httpClient = null, ILoggerFactory logFactory = null,
        IHttpClientHandlerProvider handlerProvider = null, HttpClient netHttpClient = null)
        : base(appInfoManager, logFactory)
    {
        _thisInstance = new ThisServiceInstance(instConfig);
        _configOptions = clientConfig;
        this.httpClient = httpClient ?? new EurekaHttpClientInternal(clientConfig, logFactory, handlerProvider, netHttpClient);

        InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        Applications applications = Applications;

        if (applications == null)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        IList<Application> registered = applications.GetRegisteredApplications();
        IList<string> names = new List<string>();

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

        return Task.FromResult(names);
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
        IList<IServiceInstance> instances = new List<IServiceInstance>();

        foreach (InstanceInfo info in infos)
        {
            logger?.LogDebug($"GetInstances returning: {info}");
            instances.Add(new EurekaServiceInstance(info));
        }

        return Task.FromResult(instances);
    }

    public Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_thisInstance);
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken)
    {
        AppInfoManager.InstanceStatus = InstanceStatus.Down;
        return base.ShutdownAsync(cancellationToken);
    }

    private sealed class EurekaHttpClientInternal : EurekaHttpClient
    {
        private readonly IOptionsMonitor<EurekaClientOptions> _optionsMonitorOptions;

        protected override EurekaClientConfiguration Configuration => _optionsMonitorOptions.CurrentValue;

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
