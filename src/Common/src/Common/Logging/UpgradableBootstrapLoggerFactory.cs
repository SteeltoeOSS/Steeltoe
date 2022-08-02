// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Logging;

/// <summary>
/// Allows early utilization of log infrastructure before log config is even read. Any providers spawned are instantly switched over to real log
/// providers as the application utilization progresses. This class should only be used by components that need logging infrastructure before Service
/// Container is available.
/// </summary>
internal sealed class UpgradableBootstrapLoggerFactory : IBootstrapLoggerFactory
{
    private readonly Dictionary<string, BootstrapLoggerInst> _loggers = new();

    private readonly object _lock = new();

    private readonly Action<ILoggingBuilder, IConfiguration> _bootstrapLoggingBuilder;

    private ILoggerFactory _factoryInstance;

    private ILoggerFactory _factory;

    public UpgradableBootstrapLoggerFactory()
        : this(DefaultConfigure)
    {
    }

    public UpgradableBootstrapLoggerFactory(Action<ILoggingBuilder, IConfiguration> bootstrapLoggingBuilder)
    {
        _bootstrapLoggingBuilder = bootstrapLoggingBuilder;

        _factoryInstance = LoggerFactory.Create(builder =>
        {
            IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Logging:LogLevel:Default", "Information" },
                { "Logging:LogLevel:Microsoft", "Warning" },
                { "Logging:LogLevel:Microsoft.Hosting.Lifetime", "Information" }
            }).AddEnvironmentVariables().AddCommandLine(Environment.GetCommandLineArgs()).Build();

            _bootstrapLoggingBuilder(builder, config);
        });
    }

    /// <summary>
    /// Updates existing loggers to use configuration from the supplied config.
    /// </summary>
    public void Update(IConfiguration value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (_factory != null)
        {
            return;
        }

        ILoggerFactory newLogger = LoggerFactory.Create(builder => _bootstrapLoggingBuilder(builder, value));
        Update(newLogger);
    }

    /// <summary>
    /// Updates existing loggers to use final LoggerFactory as constructed by DI container.
    /// </summary>
    public void Update(ILoggerFactory value)
    {
        if (value == null || value == _factoryInstance)
        {
            return;
        }

        lock (_lock)
        {
            _factoryInstance.Dispose();
            _factoryInstance = value;

            foreach (BootstrapLoggerInst logger in _loggers.Values)
            {
                logger.Logger = _factoryInstance.CreateLogger(logger.Name);
            }
        }

        _factory = value;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _factoryInstance.Dispose();
        }
    }

    public void AddProvider(ILoggerProvider provider)
    {
        lock (_lock)
        {
            _factoryInstance.AddProvider(provider);
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        lock (_lock)
        {
            if (!_loggers.TryGetValue(categoryName, out BootstrapLoggerInst logger))
            {
                ILogger innerLogger = _factoryInstance.CreateLogger(categoryName);
                logger = new BootstrapLoggerInst(innerLogger, categoryName);
                _loggers.Add(categoryName, logger);
            }

            return logger;
        }
    }

    private static void DefaultConfigure(ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddConsole().AddConfiguration(configuration.GetSection("Logging"));
    }

    internal sealed class BootstrapLoggerInst : ILogger
    {
        public volatile ILogger Logger;

        public string Name { get; }

        public BootstrapLoggerInst(ILogger logger, string name)
        {
            Name = name;
            Logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return Logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return Logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
