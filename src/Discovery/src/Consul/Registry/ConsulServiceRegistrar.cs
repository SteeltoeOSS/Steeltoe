// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Util;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// Registers and deregisters this app in a Consul server, and keeps the TTL heartbeat of that registration alive.
/// </summary>
internal sealed partial class ConsulServiceRegistrar : IAsyncDisposable
{
    private readonly IConsulClient _client;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ConsulServiceRegistrar> _logger;

    private bool _isRegistered;

    internal bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the single heartbeat of the registration, or <c>null</c> when none is running.
    /// </summary>
    internal PeriodicHeartbeat? Heartbeat { get; private set; }

    /// <summary>
    /// Gets the service instance that represents this app.
    /// </summary>
    public ConsulServiceInstance ServiceInstance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistrar" /> class.
    /// </summary>
    /// <param name="client">
    /// The Consul client.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public ConsulServiceRegistrar(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _client = client;
        _optionsMonitor = optionsMonitor;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ConsulServiceRegistrar>();
        ServiceInstance = ConsulServiceInstance.FromOptions(optionsMonitor.CurrentValue);
    }

    /// <summary>
    /// Registers this app with Consul, according to the current options.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // One snapshot for the entire start, so a concurrent options change cannot make the steps below disagree.
        ConsulDiscoveryOptions optionsSnapshot = _optionsMonitor.CurrentValue;

        if (!optionsSnapshot.Enabled)
        {
            LogDiscoveryClientTurnedOff();
        }
        else if (!IsRunning)
        {
            IsRunning = true;

            if (optionsSnapshot.Register)
            {
                await RegisterWithHeartbeatAsync(optionsSnapshot, cancellationToken);
            }
            else
            {
                LogRegistrationTurnedOff();
            }
        }
    }

    private async Task RegisterWithHeartbeatAsync(ConsulDiscoveryOptions optionsSnapshot, CancellationToken cancellationToken)
    {
        AgentServiceRegistration registration = CreateAgentRegistration(ServiceInstance, optionsSnapshot);

        // Created (and subscribed to options changes) before registering, so an interval change during registration is not lost.
        PeriodicHeartbeat? heartbeat = CreateHeartbeat(optionsSnapshot);

        try
        {
            await RegisterWithRetryAsync(registration, optionsSnapshot, cancellationToken);

            if (_isRegistered && heartbeat != null)
            {
                // Ownership moves to this class, which releases it in ShutdownAsync.
                heartbeat.Start();
                Heartbeat = heartbeat;
                heartbeat = null;
            }
        }
        finally
        {
            // Registration failed or was canceled, so nothing will ever use the heartbeat. Release it (and its subscription).
            if (heartbeat != null)
            {
                await heartbeat.DisposeAsync();
            }
        }
    }

    private PeriodicHeartbeat? CreateHeartbeat(ConsulDiscoveryOptions optionsSnapshot)
    {
        PeriodicHeartbeat? heartbeat = null;

        if (optionsSnapshot is { IsHeartbeatEnabled: true, Heartbeat: not null })
        {
            string checkId = $"service:{ServiceInstance.InstanceId}";
            TimeSpan interval = optionsSnapshot.Heartbeat.ComputeHeartbeatInterval();
            ILogger<PeriodicHeartbeat> heartbeatLogger = _loggerFactory.CreateLogger<PeriodicHeartbeat>();

            heartbeat = new PeriodicHeartbeat(checkId, interval, _optionsMonitor, _client, heartbeatLogger);
        }

        return heartbeat;
    }

    private async Task RegisterWithRetryAsync(AgentServiceRegistration registration, ConsulDiscoveryOptions optionsSnapshot,
        CancellationToken cancellationToken)
    {
        if (optionsSnapshot is { IsRetryEnabled: true, FailFast: true })
        {
            await DoWithRetryAsync(() => RegisterAsync(registration, optionsSnapshot, cancellationToken), optionsSnapshot.Retry, cancellationToken);
        }
        else
        {
            await RegisterAsync(registration, optionsSnapshot, cancellationToken);
        }
    }

    private async Task RegisterAsync(AgentServiceRegistration registration, ConsulDiscoveryOptions optionsSnapshot, CancellationToken cancellationToken)
    {
        LogRegistering(ServiceInstance.ServiceId);

        try
        {
            await _client.Agent.ServiceRegister(registration, cancellationToken);
            _isRegistered = true;
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            if (optionsSnapshot.FailFast)
            {
                LogRegisterFailed(exception, ServiceInstance.ServiceId);
                throw;
            }

            LogWarnForRegisterFailed(exception, ServiceInstance.ServiceId);
        }
    }

    /// <summary>
    /// Stops the heartbeat and deregisters this app from Consul, according to the current options. Safe to call multiple times.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            IsRunning = false;

            await StopHeartbeatAsync();

            if (_isRegistered)
            {
                _isRegistered = false;

                if (_optionsMonitor.CurrentValue.Deregister)
                {
                    await DeregisterAsync(cancellationToken);
                }
                else
                {
                    LogDeregistrationTurnedOff();
                }
            }
        }
    }

    private async Task StopHeartbeatAsync()
    {
        PeriodicHeartbeat? heartbeat = Heartbeat;
        Heartbeat = null;

        if (heartbeat != null)
        {
            await heartbeat.DisposeAsync();
        }
    }

    private async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        try
        {
            LogDeregistering(ServiceInstance.InstanceId);
            await _client.Agent.ServiceDeregister(ServiceInstance.InstanceId, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            LogDeregisterFailed(exception, ServiceInstance.ServiceId);
        }
    }

    private async Task DoWithRetryAsync(Func<Task> retryable, ConsulRetryOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(retryable);

        LogStartingRetryableAction();

        int attempts = 0;
        int backOff = options.InitialInterval;

        do
        {
            try
            {
                await retryable();
                LogRetryableActionFinished();
                return;
            }
            catch (Exception exception) when (!exception.IsCancellation())
            {
                attempts++;

                if (attempts < options.MaxAttempts)
                {
                    LogStartingRetry(exception, attempts);
                    await Task.Delay(backOff, cancellationToken);
                    int nextBackOff = (int)(options.Multiplier * backOff);
                    backOff = Math.Min(nextBackOff, options.MaxInterval);
                }
                else
                {
                    LogRetryFailed(exception, attempts);
                    throw;
                }
            }
        }
        while (true);
    }

    internal static AgentServiceRegistration CreateAgentRegistration(ConsulServiceInstance serviceInstance, ConsulDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceInstance);
        ArgumentNullException.ThrowIfNull(options);

        var registration = new AgentServiceRegistration
        {
            ID = serviceInstance.InstanceId,
            Address = serviceInstance.Host,
            Name = serviceInstance.ServiceId,
            Tags = [.. serviceInstance.Tags],
            Meta = serviceInstance.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value)
        };

        if (serviceInstance.Port > 0)
        {
            registration.Port = serviceInstance.Port;

            if (options.RegisterHealthCheck)
            {
                registration.Check = CreateCheck(serviceInstance.Port, options);
            }
        }

        return registration;
    }

    internal static AgentServiceCheck CreateCheck(int port, ConsulDiscoveryOptions options)
    {
        var check = new AgentServiceCheck();

        if (!string.IsNullOrEmpty(options.HealthCheckCriticalTimeout))
        {
            check.DeregisterCriticalServiceAfter = DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout);
        }

        if (options is { IsHeartbeatEnabled: true, Heartbeat: not null })
        {
            check.TTL = DateTimeConversions.ToTimeSpan(options.Heartbeat.TimeToLive);
            return check;
        }

        if (!string.IsNullOrEmpty(options.HealthCheckUrl))
        {
            check.HTTP = options.HealthCheckUrl;
        }
        else
        {
            var uri = new Uri($"{options.EffectiveScheme}://{options.HostName}:{port}{options.HealthCheckPath}");
            check.HTTP = uri.ToString();
        }

        check.Header = new Dictionary<string, List<string>>
        {
            // Override Management:Endpoints:UseStatusCodeFromResponse, Consul only looks at HTTP status code.
            ["X-Use-Status-Code-From-Response"] = ["true"]
        };

        if (!string.IsNullOrEmpty(options.HealthCheckInterval))
        {
            check.Interval = DateTimeConversions.ToTimeSpan(options.HealthCheckInterval);
        }

        if (!string.IsNullOrEmpty(options.HealthCheckTimeout))
        {
            check.Timeout = DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout);
        }

        check.TLSSkipVerify = options.HealthCheckTlsSkipVerify;
        return check;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync(CancellationToken.None);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul discovery client is turned off.")]
    private partial void LogDiscoveryClientTurnedOff();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul registration is turned off.")]
    private partial void LogRegistrationTurnedOff();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul deregistration is turned off.")]
    private partial void LogDeregistrationTurnedOff();

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting retryable action.")]
    private partial void LogStartingRetryableAction();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Finished retryable action.")]
    private partial void LogRetryableActionFinished();

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception during {Attempt} attempts of retryable action, will retry.")]
    private partial void LogStartingRetry(Exception exception, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception during {Attempt} attempts of retryable action, done with retries.")]
    private partial void LogRetryFailed(Exception exception, int attempt);
}
