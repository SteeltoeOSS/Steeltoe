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

/// <summary>
/// A discovery client for
/// <see href="https://spring.io/guides/gs/service-registration-and-discovery/">
/// Spring Cloud Eureka
/// </see>
/// .
/// </summary>
public sealed class EurekaDiscoveryClient : DiscoveryClient, IDiscoveryClient, IAsyncDisposable
{
    private readonly IOptionsMonitor<EurekaClientOptions> _clientOptionsMonitor;
    private readonly IServiceInstance _thisInstance;
    private readonly ILogger<EurekaDiscoveryClient> _logger;

    public override EurekaClientOptions ClientOptions => _clientOptionsMonitor.CurrentValue;

    public string Description => "A discovery client for Spring Cloud Eureka.";

#nullable enable

    public EurekaDiscoveryClient(IOptionsMonitor<EurekaClientOptions> clientOptionsMonitor, IOptionsMonitor<EurekaInstanceOptions> instanceOptionsMonitor,
        EurekaApplicationInfoManager appInfoManager, EurekaHttpClient eurekaHttpClient, ILoggerFactory loggerFactory, HttpClient? httpClient = null)
        : base(appInfoManager, loggerFactory)
    {
        ArgumentGuard.NotNull(clientOptionsMonitor);
        ArgumentGuard.NotNull(instanceOptionsMonitor);
        ArgumentGuard.NotNull(eurekaHttpClient);

        _clientOptionsMonitor = clientOptionsMonitor;
        _thisInstance = new ThisServiceInstance(instanceOptionsMonitor);
        this.httpClient = eurekaHttpClient;
        _logger = loggerFactory.CreateLogger<EurekaDiscoveryClient>();

        InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

#nullable disable

    /// <inheritdoc />
    public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
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

    /// <inheritdoc />
    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
        IList<IServiceInstance> instances = new List<IServiceInstance>();

        foreach (InstanceInfo info in infos)
        {
            _logger.LogDebug($"GetInstances returning: {info}");
            instances.Add(new EurekaServiceInstance(info));
        }

        return Task.FromResult(instances);
    }

    /// <inheritdoc />
    public IServiceInstance GetLocalServiceInstance()
    {
        return _thisInstance;
    }

    /// <inheritdoc cref="IDiscoveryClient.ShutdownAsync" />
    public override Task ShutdownAsync(CancellationToken cancellationToken)
    {
        AppInfoManager.InstanceStatus = InstanceStatus.Down;
        return base.ShutdownAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync(CancellationToken.None);
    }
}
