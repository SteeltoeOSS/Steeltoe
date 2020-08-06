// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using System.Threading;

namespace Steeltoe.Discovery.Consul.Registry
{
    /// <summary>
    /// A registrar used to register a service in a Consul server
    /// </summary>
    public class ConsulServiceRegistrar : IConsulServiceRegistrar
    {
        internal int _running = 0;

        private const int NOT_RUNNING = 0;
        private const int RUNNING = 1;

        private readonly ILogger<ConsulServiceRegistrar> _logger;
        private readonly IConsulServiceRegistry _registry;
        private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
        private readonly ConsulDiscoveryOptions _options;

        /// <inheritdoc/>
        public IConsulRegistration Registration { get; }

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
        /// Initializes a new instance of the <see cref="ConsulServiceRegistrar"/> class.
        /// </summary>
        /// <param name="registry">the Consul service registry to use when doing registrations</param>
        /// <param name="optionsMonitor">configuration options to use</param>
        /// <param name="registration">the registration to register with Consul</param>
        /// <param name="logger">optional logger</param>
        public ConsulServiceRegistrar(IConsulServiceRegistry registry, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IConsulRegistration registration, ILogger<ConsulServiceRegistrar> logger = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulServiceRegistrar"/> class.
        /// </summary>
        /// <param name="registry">the Consul service registry to use when doing registrations</param>
        /// <param name="options">configuration options to use</param>
        /// <param name="registration">the registration to register with Consul</param>
        /// <param name="logger">optional logger</param>
        public ConsulServiceRegistrar(IConsulServiceRegistry registry, ConsulDiscoveryOptions options, IConsulRegistration registration, ILogger<ConsulServiceRegistrar> logger = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (!Options.Enabled)
            {
                _logger?.LogDebug("Discovery Lifecycle disabled.Not starting");
                return;
            }

            if (Interlocked.CompareExchange(ref _running, RUNNING, NOT_RUNNING) == NOT_RUNNING)
            {
                if (Options.IsRetryEnabled && Options.FailFast)
                {
                    DoWithRetry(Register, Options.Retry);
                }
                else
                {
                    Register();
                }
            }
        }

        /// <inheritdoc/>
        public void Register()
        {
            if (!Options.Register)
            {
                _logger?.LogDebug("Registration disabled");
                return;
            }

            _registry.Register(Registration);
        }

        /// <inheritdoc/>
        public void Deregister()
        {
            if (!Options.Register || !Options.Deregister)
            {
                _logger?.LogDebug("Deregistration disabled");
                return;
            }

            _registry.Deregister(Registration);
        }

        private void DoWithRetry(Action retryable, ConsulRetryOptions options)
        {
            if (retryable == null)
            {
                throw new ArgumentNullException(nameof(retryable));
            }

            _logger?.LogDebug("Starting retryable action ..");

            var attempts = 0;
            var backOff = options.InitialInterval;
            do
            {
                try
                {
                    retryable();
                    _logger?.LogDebug("Finished retryable action ..");
                    return;
                }
                catch (Exception e)
                {
                    attempts++;
                    if (attempts < options.MaxAttempts)
                    {
                        _logger?.LogError(e, "Exception during {attempt} attempts of retryable action, will retry", attempts);
                        Thread.CurrentThread.Join(backOff);
                        var nextBackoff = (int)(backOff * options.Multiplier);
                        backOff = Math.Min(nextBackoff, options.MaxInterval);
                    }
                    else
                    {
                        _logger?.LogError(e, "Exception during {attempt} attempts of retryable action, done with retry", attempts);
                        throw;
                    }
                }
            }
            while (true);
        }

        private bool _disposed = false;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Cleanup
                    if (Interlocked.CompareExchange(ref _running, NOT_RUNNING, RUNNING) == RUNNING)
                    {
                        Deregister();
                    }

                    _registry.Dispose();
                }

                _disposed = true;
            }
        }

        ~ConsulServiceRegistrar()
        {
            Dispose(false);
        }
    }
}
