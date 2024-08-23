// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// A service registry that uses Consul.
/// </summary>
public sealed class ConsulServiceRegistry : IAsyncDisposable
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
    /// Registers the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// The registration to register.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task RegisterAsync(ConsulRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        _logger.LogInformation("Registering service {ServiceId} with Consul.", registration.ServiceId);

        try
        {
            await _client.Agent.ServiceRegister(registration.InnerRegistration, cancellationToken);

            if (Options.IsHeartbeatEnabled && _scheduler != null)
            {
                _scheduler.Add(registration.InstanceId);
            }
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            if (Options.FailFast)
            {
                _logger.LogError(exception, "Error registering service {ServiceId} with Consul.", registration.ServiceId);
                throw;
            }

            _logger.LogWarning(exception, "FailFast is false. Error registering service {ServiceId} with Consul.", registration.ServiceId);
        }
    }

    /// <summary>
    /// Deregisters the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// The registration to deregister.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task DeregisterAsync(ConsulRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        try
        {
            if (Options.IsHeartbeatEnabled && _scheduler != null)
            {
                await _scheduler.RemoveAsync(registration.InstanceId);
            }

            _logger.LogInformation("Deregistering service {InstanceId} with Consul.", registration.InstanceId);
            await _client.Agent.ServiceDeregister(registration.InstanceId, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error deregistering service {ServiceId} with Consul.", registration.ServiceId);
        }
    }

    /// <summary>
    /// Sets the status of the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// The registration whose status to set.
    /// </param>
    /// <param name="status">
    /// The status value to set.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task SetStatusAsync(ConsulRegistration registration, string status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (string.Equals(status, OutOfService, StringComparison.OrdinalIgnoreCase))
        {
            await _client.Agent.EnableServiceMaintenance(registration.InstanceId, OutOfService, cancellationToken);
        }
        else if (string.Equals(status, Up, StringComparison.OrdinalIgnoreCase))
        {
            await _client.Agent.DisableServiceMaintenance(registration.InstanceId, cancellationToken);
        }
        else
        {
            throw new ArgumentException($"Unknown status: {status}", nameof(status));
        }
    }

    /// <summary>
    /// Gets the status of the provided registration in Consul.
    /// </summary>
    /// <param name="registration">
    /// The registration whose status to obtain.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task<string> GetStatusAsync(ConsulRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(registration);

        QueryResult<HealthCheck[]> queryResult = await _client.Health.Checks(registration.ServiceId, QueryOptions.Default, cancellationToken);

        foreach (HealthCheck check in queryResult.Response)
        {
            if (check.ServiceID == registration.InstanceId && string.Equals(check.Name, "Service Maintenance Mode", StringComparison.OrdinalIgnoreCase))
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
}
