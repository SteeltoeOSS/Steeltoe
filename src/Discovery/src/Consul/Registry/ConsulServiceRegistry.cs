// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Consul.Configuration;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// A service registry that uses Consul.
/// </summary>
internal sealed partial class ConsulServiceRegistry : IAsyncDisposable
{
    private const string Up = "UP";
    private const string OutOfService = "OUT_OF_SERVICE";

    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly TtlScheduler? _scheduler;
    private readonly ILogger<ConsulServiceRegistry> _logger;

    private ConsulDiscoveryOptions Options => _optionsMonitor.CurrentValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistry" /> class.
    /// </summary>
    /// <param name="client">
    /// The Consul client to use.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="scheduler">
    /// An optional scheduler to use for heartbeats.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public ConsulServiceRegistry(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, TtlScheduler? scheduler,
        ILogger<ConsulServiceRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _scheduler = scheduler;
        _logger = logger;
    }

    /// <summary>
    /// Registers the provided service instance in Consul.
    /// </summary>
    /// <param name="registration">
    /// The Consul payload to submit for the service instance.
    /// </param>
    /// <param name="instance">
    /// The service instance to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task RegisterAsync(AgentServiceRegistration registration, ConsulServiceInstance instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(registration);

        LogRegistering(instance.ServiceId);

        try
        {
            await _client.Agent.ServiceRegister(registration, cancellationToken);

            if (Options.IsHeartbeatEnabled && _scheduler != null)
            {
                _scheduler.Add(instance.InstanceId);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            if (Options.FailFast)
            {
                LogRegisterFailed(exception, instance.ServiceId);
                throw;
            }

            LogWarnForRegisterFailed(exception, instance.ServiceId);
        }
    }

    /// <summary>
    /// Deregisters the provided service instance in Consul.
    /// </summary>
    /// <param name="instance">
    /// The service instance to deregister.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task DeregisterAsync(ConsulServiceInstance instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);

        try
        {
            if (Options.IsHeartbeatEnabled && _scheduler != null)
            {
                await _scheduler.RemoveAsync(instance.InstanceId);
            }

            LogDeregistering(instance.InstanceId);
            await _client.Agent.ServiceDeregister(instance.InstanceId, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            LogDeregisterFailed(exception, instance.ServiceId);
        }
    }

    /// <summary>
    /// Sets the status of the provided service instance in Consul.
    /// </summary>
    /// <param name="instance">
    /// The service instance whose status to set.
    /// </param>
    /// <param name="status">
    /// The status value to set.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task SetStatusAsync(ConsulServiceInstance instance, string status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (string.Equals(status, OutOfService, StringComparison.OrdinalIgnoreCase))
        {
            await _client.Agent.EnableServiceMaintenance(instance.InstanceId, OutOfService, cancellationToken);
        }
        else if (string.Equals(status, Up, StringComparison.OrdinalIgnoreCase))
        {
            await _client.Agent.DisableServiceMaintenance(instance.InstanceId, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Unknown status: {status}", nameof(status));
        }
    }

    /// <summary>
    /// Gets the status of the provided service instance in Consul.
    /// </summary>
    /// <param name="instance">
    /// The service instance whose status to obtain.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task<string> GetStatusAsync(ConsulServiceInstance instance, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);

        QueryResult<HealthCheck[]> queryResult = await _client.Health.Checks(instance.ServiceId, QueryOptions.Default, cancellationToken);

        foreach (HealthCheck check in queryResult.Response)
        {
            if (check.ServiceID == instance.InstanceId && string.Equals(check.Name, "Service Maintenance Mode", StringComparison.OrdinalIgnoreCase))
            {
                return OutOfService;
            }
        }

        return Up;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_scheduler != null)
        {
            await _scheduler.DisposeAsync();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Registering service {ServiceId} with Consul.")]
    private partial void LogRegistering(string serviceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error registering service {ServiceId} with Consul.")]
    private partial void LogRegisterFailed(Exception exception, string serviceId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FailFast is false. Error registering service {ServiceId} with Consul.")]
    private partial void LogWarnForRegisterFailed(Exception exception, string serviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deregistering service {InstanceId} with Consul.")]
    private partial void LogDeregistering(string instanceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error deregistering service {ServiceId} with Consul.")]
    private partial void LogDeregisterFailed(Exception exception, string serviceId);
}
