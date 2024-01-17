// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// A registrar used to register a service in a Consul server.
/// </summary>
public sealed class ConsulServiceRegistrar : IAsyncDisposable
{
    private const int NotRunning = 0;
    private const int Running = 1;

    private readonly ConsulServiceRegistry _registry;
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ILogger<ConsulServiceRegistrar> _logger;

    private bool _isDisposed;
    internal int IsRunning;

    private ConsulDiscoveryOptions Options => _optionsMonitor.CurrentValue;

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
    /// Provides access to Consul configuration options.
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
        ArgumentGuard.NotNull(registry);
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(registration);
        ArgumentGuard.NotNull(logger);

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
            _logger.LogDebug("Discovery Lifecycle disabled. Not starting");
            return;
        }

        if (Interlocked.CompareExchange(ref IsRunning, Running, NotRunning) == NotRunning)
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
            _logger.LogDebug("Registration disabled");
            return;
        }

        await _registry.RegisterAsync(Registration, cancellationToken);
    }

    private async Task DeregisterAsync(CancellationToken cancellationToken)
    {
        if (!Options.Register || !Options.Deregister)
        {
            _logger.LogDebug("Deregistration disabled");
            return;
        }

        await _registry.DeregisterAsync(Registration, cancellationToken);
    }

    private async Task DoWithRetryAsync(Func<CancellationToken, Task> retryable, ConsulRetryOptions options, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(retryable);

        _logger.LogDebug("Starting retryable action ..");

        int attempts = 0;
        int backOff = options.InitialInterval;

        do
        {
            try
            {
                await retryable(cancellationToken);
                _logger.LogDebug("Finished retryable action ..");
                return;
            }
            catch (Exception exception)
            {
                attempts++;

                if (attempts < options.MaxAttempts)
                {
                    _logger.LogError(exception, "Exception during {attempt} attempts of retryable action, will retry", attempts);
                    Thread.CurrentThread.Join(backOff);
                    int nextBackOff = (int)(backOff * options.Multiplier);
                    backOff = Math.Min(nextBackOff, options.MaxInterval);
                }
                else
                {
                    _logger.LogError(exception, "Exception during {attempt} attempts of retryable action, done with retry", attempts);
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
            if (Interlocked.CompareExchange(ref IsRunning, NotRunning, Running) == Running)
            {
                await DeregisterAsync(CancellationToken.None);
            }

            _registry.Dispose();
            _isDisposed = true;
        }
    }
}
