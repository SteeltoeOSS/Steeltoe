// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// An implementation of a Consul service registry.
/// </summary>
public class ConsulServiceRegistry : IConsulServiceRegistry
{
    private const string Up = "UP";
    private const string OutOfService = "OUT_OF_SERVICE";

    private readonly IConsulClient _client;
    private readonly IScheduler _scheduler;
    private readonly ILogger<ConsulServiceRegistry> _logger;

    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;

    internal ConsulDiscoveryOptions Options
    {
        get
        {
            if (_optionsMonitor != null)
            {
                return _optionsMonitor.CurrentValue;
            }

            return _options;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistry" /> class.
    /// </summary>
    /// <param name="client">
    /// the Consul client to use.
    /// </param>
    /// <param name="options">
    /// the configuration options.
    /// </param>
    /// <param name="scheduler">
    /// a scheduler to use for heart beats.
    /// </param>
    /// <param name="logger">
    /// an optional logger.
    /// </param>
    public ConsulServiceRegistry(IConsulClient client, ConsulDiscoveryOptions options, IScheduler scheduler = null,
        ILogger<ConsulServiceRegistry> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(options);

        _client = client;
        _options = options;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistry" /> class.
    /// </summary>
    /// <param name="client">
    /// the Consul client to use.
    /// </param>
    /// <param name="optionsMonitor">
    /// the configuration options.
    /// </param>
    /// <param name="scheduler">
    /// a scheduler to use for heart beats.
    /// </param>
    /// <param name="logger">
    /// an optional logger.
    /// </param>
    public ConsulServiceRegistry(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IScheduler scheduler = null,
        ILogger<ConsulServiceRegistry> logger = null)
    {
        ArgumentGuard.NotNull(client);
        ArgumentGuard.NotNull(optionsMonitor);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task RegisterAsync(IConsulRegistration registration)
    {
        ArgumentGuard.NotNull(registration);

        return RegisterInternalAsync(registration);
    }

    private async Task RegisterInternalAsync(IConsulRegistration registration)
    {
        _logger?.LogInformation("Registering service with consul {serviceId} ", registration.ServiceId);

        try
        {
            await _client.Agent.ServiceRegister(registration.Service).ConfigureAwait(false);

            if (Options.IsHeartBeatEnabled && _scheduler != null)
            {
                _scheduler.Add(registration.InstanceId);
            }
        }
        catch (Exception e)
        {
            if (Options.FailFast)
            {
                _logger?.LogError(e, "Error registering service with consul {serviceId} ", registration.ServiceId);
                throw;
            }

            _logger?.LogWarning(e, "Failfast is false. Error registering service with consul {serviceId} ", registration.ServiceId);
        }
    }

    /// <inheritdoc />
    public Task DeregisterAsync(IConsulRegistration registration)
    {
        ArgumentGuard.NotNull(registration);

        return DeregisterInternalAsync(registration);
    }

    private Task DeregisterInternalAsync(IConsulRegistration registration)
    {
        if (Options.IsHeartBeatEnabled && _scheduler != null)
        {
            _scheduler.Remove(registration.InstanceId);
        }

        _logger?.LogInformation("Deregistering service with consul {instanceId} ", registration.InstanceId);

        return _client.Agent.ServiceDeregister(registration.InstanceId);
    }

    /// <inheritdoc />
    public Task SetStatusAsync(IConsulRegistration registration, string status)
    {
        ArgumentGuard.NotNull(registration);

        return SetStatusInternalAsync(registration, status);
    }

    private Task SetStatusInternalAsync(IConsulRegistration registration, string status)
    {
        if (OutOfService.Equals(status, StringComparison.OrdinalIgnoreCase))
        {
            return _client.Agent.EnableServiceMaintenance(registration.InstanceId, OutOfService);
        }

        if (Up.Equals(status, StringComparison.OrdinalIgnoreCase))
        {
            return _client.Agent.DisableServiceMaintenance(registration.InstanceId);
        }

        throw new ArgumentException($"Unknown status: {status}", nameof(status));
    }

    /// <inheritdoc />
    public Task<object> GetStatusAsync(IConsulRegistration registration)
    {
        ArgumentGuard.NotNull(registration);

        return GetStatusInternalAsync(registration);
    }

    public async Task<object> GetStatusInternalAsync(IConsulRegistration registration)
    {
        QueryResult<HealthCheck[]> response = await _client.Health.Checks(registration.ServiceId, QueryOptions.Default).ConfigureAwait(false);
        HealthCheck[] checks = response.Response;

        foreach (HealthCheck check in checks)
        {
            if (check.ServiceID.Equals(registration.InstanceId) && check.Name.Equals("Service Maintenance Mode", StringComparison.OrdinalIgnoreCase))
            {
                return OutOfService;
            }
        }

        return Up;
    }

    /// <inheritdoc />
    public void Register(IConsulRegistration registration)
    {
        RegisterAsync(registration).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Deregister(IConsulRegistration registration)
    {
        DeregisterAsync(registration).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void SetStatus(IConsulRegistration registration, string status)
    {
        SetStatusAsync(registration, status).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public TStatus GetStatus<TStatus>(IConsulRegistration registration)
        where TStatus : class
    {
        object result = GetStatusAsync(registration).GetAwaiter().GetResult();

        return (TStatus)result;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scheduler?.Dispose();
        }
    }
}
