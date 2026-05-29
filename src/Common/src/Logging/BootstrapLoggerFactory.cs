// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Common.Logging;

/// <summary>
/// Provides early logging at application initialization, before configuration has been loaded, the service container has been built, and the app has
/// started. Once the app has started, the created loggers can be upgraded to utilize loaded configuration and the service container by calling
/// <see cref="BootstrapLoggerServiceCollectionExtensions.UpgradeBootstrapLoggerFactory" />.
/// </summary>
public sealed class BootstrapLoggerFactory : ILoggerFactory
{
    private static readonly Action<ILoggingBuilder> ConfigureConsole = loggingBuilder =>
    {
        loggingBuilder.SetMinimumLevel(LogLevel.Trace);
#pragma warning disable S4792 // Configuring loggers is security-sensitive
        loggingBuilder.AddConsole(options => options.MaxQueueLength = 1);
#pragma warning restore S4792 // Configuring loggers is security-sensitive

        var appSettings = new Dictionary<string, string?>
        {
            ["LogLevel:Default"] = "Information"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        loggingBuilder.AddConfiguration(configuration);
    };

    private readonly LockPrimitive _lock = new();
    private readonly Dictionary<string, UpgradableLogger> _loggersByCategoryName = [];
    private ILoggerFactory _innerFactory;

    private BootstrapLoggerFactory(Action<ILoggingBuilder> configure)
    {
        _innerFactory = LoggerFactory.Create(configure);
    }

    /// <summary>
    /// Creates a new <see cref="BootstrapLoggerFactory" /> that writes to the console.
    /// </summary>
    public static BootstrapLoggerFactory CreateConsole()
    {
        return CreateEmpty(ConfigureConsole);
    }

    /// <summary>
    /// Creates a new <see cref="BootstrapLoggerFactory" /> that writes to the console.
    /// </summary>
    /// <param name="configure">
    /// Enables further configuring the bootstrap logger from code.
    /// </param>
    public static BootstrapLoggerFactory CreateConsole(Action<ILoggingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return CreateEmpty(loggingBuilder =>
        {
            ConfigureConsole(loggingBuilder);
            configure(loggingBuilder);
        });
    }

    /// <summary>
    /// Creates a new empty <see cref="BootstrapLoggerFactory" />.
    /// </summary>
    /// <param name="configure">
    /// Enables fully configuring the bootstrap logger from code.
    /// </param>
    public static BootstrapLoggerFactory CreateEmpty(Action<ILoggingBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

#pragma warning disable S4792 // Configuring loggers is security-sensitive
        return new BootstrapLoggerFactory(configure);
#pragma warning restore S4792 // Configuring loggers is security-sensitive
    }

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        lock (_lock)
        {
            _innerFactory.AddProvider(provider);
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);

        lock (_lock)
        {
            if (!_loggersByCategoryName.TryGetValue(categoryName, out UpgradableLogger? logger))
            {
                ILogger innerLogger;

                try
                {
                    innerLogger = _innerFactory.CreateLogger(categoryName);
                }
                catch (ObjectDisposedException)
                {
                    // This happens when multiple tests are running in parallel, each creating their own service container, but sharing a single BootstrapLoggerFactory instance.
                    // When the first service container gets disposed, it disposes its contained BootstrapLoggerFactory instance, which makes subsequent tests fail.
                    throw new InvalidOperationException($"{nameof(BootstrapLoggerFactory)} is not thread-safe. Do not share a single instance.");
                }

                logger = new UpgradableLogger(innerLogger, categoryName);
                _loggersByCategoryName.Add(categoryName, logger);
            }

            return logger;
        }
    }

    /// <summary>
    /// Upgrades the active loggers from new instances obtained from the service container.
    /// </summary>
    /// <param name="loggerFactory">
    /// The logger factory from the service container.
    /// </param>
    public void Upgrade(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        if (loggerFactory == _innerFactory)
        {
            return;
        }

        lock (_lock)
        {
            foreach (UpgradableLogger logger in _loggersByCategoryName.Values)
            {
                ILogger replacementLogger = loggerFactory.CreateLogger(logger.CategoryName);
                logger.Upgrade(replacementLogger);
            }

            _innerFactory.Dispose();
            _innerFactory = loggerFactory;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _innerFactory.Dispose();
    }
}
