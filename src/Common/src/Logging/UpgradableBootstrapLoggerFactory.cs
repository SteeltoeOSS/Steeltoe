// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Logging;

/// <summary>
/// Allows early utilization of log infrastructure before log configuration is even read. Any providers spawned are instantly switched over to real log
/// providers as the application utilization progresses. This class should only be used by components that need logging infrastructure before Service
/// Container is available.
/// </summary>
internal sealed class UpgradableBootstrapLoggerFactory : IBootstrapLoggerFactory
{
    private readonly Dictionary<string, BootstrapLoggerInstance> _loggersByCategoryName = new();
    private readonly object _lock = new();
    private readonly Action<ILoggingBuilder, IConfiguration> _bootstrapLoggingBuilder;

    private ILoggerFactory _factoryInstance;
    private ILoggerFactory? _factory;

    public UpgradableBootstrapLoggerFactory()
        : this(DefaultConfigure)
    {
    }

    public UpgradableBootstrapLoggerFactory(Action<ILoggingBuilder, IConfiguration> bootstrapLoggingBuilder)
    {
        _bootstrapLoggingBuilder = bootstrapLoggingBuilder;

        _factoryInstance = LoggerFactory.Create(builder =>
        {
            IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Logging:LogLevel:Default", "Information" },
                { "Logging:LogLevel:Microsoft", "Warning" },
                { "Logging:LogLevel:Microsoft.Hosting.Lifetime", "Information" }
            }).AddEnvironmentVariables().AddCommandLine(Environment.GetCommandLineArgs()).Build();

            _bootstrapLoggingBuilder(builder, configurationRoot);
        });
    }

    /// <summary>
    /// Updates existing loggers to use configuration from the supplied configuration.
    /// </summary>
    public void Update(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (_factory != null)
        {
            return;
        }

        ILoggerFactory newLogger = LoggerFactory.Create(builder => _bootstrapLoggingBuilder(builder, configuration));
        Update(newLogger);
    }

    /// <summary>
    /// Updates existing loggers to use final LoggerFactory as constructed by IoC container.
    /// </summary>
    public void Update(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (loggerFactory == _factoryInstance)
        {
            return;
        }

        lock (_lock)
        {
            _factoryInstance.Dispose();
            _factoryInstance = loggerFactory;

            foreach (BootstrapLoggerInstance logger in _loggersByCategoryName.Values)
            {
                logger.UpdateLogger(_factoryInstance.CreateLogger(logger.Name));
            }
        }

        _factory = loggerFactory;
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
            if (!_loggersByCategoryName.TryGetValue(categoryName, out BootstrapLoggerInstance? logger))
            {
                ILogger innerLogger = _factoryInstance.CreateLogger(categoryName);
                logger = new BootstrapLoggerInstance(innerLogger, categoryName);
                _loggersByCategoryName.Add(categoryName, logger);
            }

            return logger;
        }
    }

    private static void DefaultConfigure(ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddConsole().AddConfiguration(configuration.GetSection("Logging"));
    }

    private sealed class BootstrapLoggerInstance : ILogger
    {
        private volatile ILogger _logger;

        public string Name { get; }

        public BootstrapLoggerInstance(ILogger logger, string name)
        {
            _logger = logger;
            Name = name;
        }

        public void UpdateLogger(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
