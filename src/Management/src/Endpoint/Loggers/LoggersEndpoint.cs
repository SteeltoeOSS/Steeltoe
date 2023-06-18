// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpoint : ILoggersEndpoint
{
    private static readonly List<string> Levels = new()
    {
        LoggerLevels.MapLogLevel(LogLevel.None),
        LoggerLevels.MapLogLevel(LogLevel.Critical),
        LoggerLevels.MapLogLevel(LogLevel.Error),
        LoggerLevels.MapLogLevel(LogLevel.Warning),
        LoggerLevels.MapLogLevel(LogLevel.Information),
        LoggerLevels.MapLogLevel(LogLevel.Debug),
        LoggerLevels.MapLogLevel(LogLevel.Trace)
    };

    private readonly ILogger<LoggersEndpoint> _logger;
    private readonly IOptionsMonitor<LoggersEndpointOptions> _options;
    private readonly IDynamicLoggerProvider _cloudFoundryLoggerProvider;

    public IEndpointOptions Options => _options.CurrentValue;

    public LoggersEndpoint(IOptionsMonitor<LoggersEndpointOptions> options, ILogger<LoggersEndpoint> logger,
        IDynamicLoggerProvider cloudFoundryLoggerProvider = null)
    {
        ArgumentGuard.NotNull(logger);

        _options = options;
        _cloudFoundryLoggerProvider = cloudFoundryLoggerProvider;
        _logger = logger;
    }

    public virtual Dictionary<string, object> Invoke(LoggersChangeRequest request)
    {
        _logger.LogDebug("Invoke({request})", SecurityUtilities.SanitizeInput(request?.ToString()));

        return DoInvoke(_cloudFoundryLoggerProvider, request);
    }

    public virtual Dictionary<string, object> DoInvoke(IDynamicLoggerProvider provider, LoggersChangeRequest request)
    {
        var result = new Dictionary<string, object>();

        if (request != null)
        {
            SetLogLevel(provider, request.Name, request.Level);
        }
        else
        {
            AddLevels(result);
            ICollection<ILoggerConfiguration> configurations = GetLoggerConfigurations(provider);
            var loggers = new Dictionary<string, LoggerLevels>();

            foreach (ILoggerConfiguration configuration in configurations.OrderBy(entry => entry.Name))
            {
                _logger.LogTrace("Adding {configuration}", configuration);
                var lv = new LoggerLevels(configuration.ConfiguredLevel, configuration.EffectiveLevel);
                loggers.Add(configuration.Name, lv);
            }

            result.Add("loggers", loggers);
        }

        return result;
    }

    public virtual void AddLevels(Dictionary<string, object> result)
    {
        result.Add("levels", Levels);
    }

    public virtual ICollection<ILoggerConfiguration> GetLoggerConfigurations(IDynamicLoggerProvider provider)
    {
        if (provider == null)
        {
            _logger.LogInformation("Unable to access the Dynamic Logging provider, log configuration unavailable");
            return new List<ILoggerConfiguration>();
        }

        return provider.GetLoggerConfigurations();
    }

    public virtual void SetLogLevel(IDynamicLoggerProvider provider, string name, string level)
    {
        if (provider == null)
        {
            _logger.LogInformation("Unable to access the Dynamic Logging provider, log level not changed");
            return;
        }

        ArgumentGuard.NotNullOrEmpty(name);

        provider.SetLogLevel(name, LoggerLevels.MapLogLevel(level));
    }

    public Dictionary<string, string> DeserializeRequest(Stream stream)
    {
        try
        {
            return (Dictionary<string, string>)JsonSerializer.DeserializeAsync(stream, typeof(Dictionary<string, string>)).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception deserializing LoggersEndpoint Request.");
        }

        return new Dictionary<string, string>();
    }
}
