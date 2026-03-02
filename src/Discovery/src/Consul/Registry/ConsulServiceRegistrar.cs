// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.Consul.Configuration;

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

    private bool _isDisposed;
    private int _isRunning;

    private ConsulDiscoveryOptions Options => _optionsMonitor.CurrentValue;

    internal bool IsRunning => _isRunning == Running;

    /// <summary>
    /// Gets the registration that the registrar is to register with Consul.
    /// </summary>
    public ConsulRegistration Registration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulServiceRegistrar" /> class.
    /// </summary>
    /// <param name="registry">
    /// The Consul service registry to use when doing registrations.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="registration">
    /// The registration to register with Consul.
    /// </param>
    /// <param name="logger">
    /// Used for internal logging. Pass <see cref="NullLogger{T}.Instance" /> to disable logging.
    /// </param>
    public ConsulServiceRegistrar(ConsulServiceRegistry registry, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, ConsulRegistration registration,
        ILogger<ConsulServiceRegistrar> logger)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(logger);

        _registry = registry;
        _optionsMonitor = optionsMonitor;
        Registration = registration;
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

        await _registry.RegisterAsync(Registration, cancellationToken);
    }

    private async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        if (!Options.Register || !Options.Deregister)
        {
            LogDeregistrationTurnedOff();
            return;
        }

        await _registry.DeregisterAsync(Registration, cancellationToken);
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
                    Thread.CurrentThread.Join(backOff);
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
