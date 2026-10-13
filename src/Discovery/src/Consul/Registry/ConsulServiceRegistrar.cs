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
/// A registrar used to register a service in a Consul server.
/// </summary>
internal sealed partial class ConsulServiceRegistrar : IAsyncDisposable
{
    private const int NotRunning = 0;
    private const int Running = 1;

    private readonly ConsulServiceRegistry _registry;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ILogger<ConsulServiceRegistrar> _logger;
    private readonly AgentServiceRegistration _agentRegistration;

    private bool _isDisposed;
    private int _isRunning;

    private ConsulDiscoveryOptions Options => _optionsMonitor.CurrentValue;

    internal bool IsRunning => _isRunning == Running;

    /// <summary>
    /// Gets the service instance that the registrar is to register with Consul.
    /// </summary>
    public ConsulServiceInstance ServiceInstance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistrar" /> class.
    /// </summary>
    /// <param name="registry">
    /// The Consul service registry to use when doing registrations.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="serviceInstance">
    /// The service instance to register with Consul.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public ConsulServiceRegistrar(ConsulServiceRegistry registry, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ConsulServiceInstance serviceInstance,
        ILogger<ConsulServiceRegistrar> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(serviceInstance);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _optionsMonitor = optionsMonitor;
        ServiceInstance = serviceInstance;
        _agentRegistration = CreateAgentRegistration(serviceInstance, optionsMonitor.CurrentValue);
        _logger = logger;
    }

    /// <summary>
    /// Starts the service registrar.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!Options.Enabled)
        {
            LogDiscoveryClientTurnedOff();
            return;
        }

        if (Interlocked.CompareExchange(ref _isRunning, Running, NotRunning) == NotRunning)
        {
            if (Options is { IsRetryEnabled: true, FailFast: true })
            {
                await DoWithRetryAsync(RegisterAsync, Options.Retry, cancellationToken);
            }
            else
            {
                await RegisterAsync(cancellationToken);
            }
        }
    }

    private async Task RegisterAsync(CancellationToken cancellationToken)
    {
        if (!Options.Register)
        {
            LogRegistrationTurnedOff();
            return;
        }

        await _registry.RegisterAsync(_agentRegistration, ServiceInstance, cancellationToken);
    }

    private async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        if (!Options.Register || !Options.Deregister)
        {
            LogDeregistrationTurnedOff();
            return;
        }

        await _registry.DeregisterAsync(ServiceInstance, cancellationToken);
    }

    private async Task DoWithRetryAsync(Func<CancellationToken, Task> retryable, ConsulRetryOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(retryable);

        LogStartingRetryableAction();

        int attempts = 0;
        int backOff = options.InitialInterval;

        do
        {
            try
            {
                await retryable(cancellationToken);
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
                    int nextBackOff = (int)(backOff * options.Multiplier);
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

        var agentRegistration = new AgentServiceRegistration
        {
            ID = serviceInstance.InstanceId,
            Address = serviceInstance.Host,
            Name = serviceInstance.ServiceId,
            Tags = serviceInstance.Tags.ToArray(),
            Meta = serviceInstance.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value)
        };

        if (serviceInstance.Port > 0)
        {
            agentRegistration.Port = serviceInstance.Port;

            if (options.RegisterHealthCheck)
            {
                agentRegistration.Check = CreateCheck(serviceInstance.Port, options);
            }
        }

        return agentRegistration;
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
        if (!_isDisposed)
        {
            if (Interlocked.CompareExchange(ref _isRunning, NotRunning, Running) == Running)
            {
                await DeregisterAsync(CancellationToken.None);
            }

            await _registry.DisposeAsync();
            _isDisposed = true;
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul discovery client is turned off.")]
    private partial void LogDiscoveryClientTurnedOff();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul registration is turned off.")]
    private partial void LogRegistrationTurnedOff();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Consul deregistration is turned off.")]
    private partial void LogDeregistrationTurnedOff();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting retryable action.")]
    private partial void LogStartingRetryableAction();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Finished retryable action.")]
    private partial void LogRetryableActionFinished();

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception during {Attempt} attempts of retryable action, will retry.")]
    private partial void LogStartingRetry(Exception exception, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception during {Attempt} attempts of retryable action, done with retries.")]
    private partial void LogRetryFailed(Exception exception, int attempt);
}
