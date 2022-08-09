// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersEndpoint : AbstractEndpoint<Dictionary<string, object>, LoggersChangeRequest>, ILoggersEndpoint
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
    private readonly IDynamicLoggerProvider _cloudFoundryLoggerProvider;

    protected new ILoggersOptions Options => options as ILoggersOptions;

    public LoggersEndpoint(ILoggersOptions options, IDynamicLoggerProvider cloudFoundryLoggerProvider = null, ILogger<LoggersEndpoint> logger = null)
        : base(options)
    {
        _cloudFoundryLoggerProvider = cloudFoundryLoggerProvider;
        _logger = logger;
    }

    public override Dictionary<string, object> Invoke(LoggersChangeRequest request)
    {
        _logger?.LogDebug("Invoke({0})", SecurityUtilities.SanitizeInput(request?.ToString()));

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
            ICollection<ILoggerConfiguration> configuration = GetLoggerConfigurations(provider);
            var loggers = new Dictionary<string, LoggerLevels>();

            foreach (ILoggerConfiguration c in configuration.OrderBy(entry => entry.Name))
            {
                _logger.LogTrace("Adding " + c);
                var lv = new LoggerLevels(c.ConfiguredLevel, c.EffectiveLevel);
                loggers.Add(c.Name, lv);
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
            _logger?.LogInformation("Unable to access the Dynamic Logging provider, log configuration unavailable");
            return new List<ILoggerConfiguration>();
        }

        return provider.GetLoggerConfigurations();
    }

    public virtual void SetLogLevel(IDynamicLoggerProvider provider, string name, string level)
    {
        if (provider == null)
        {
            _logger?.LogInformation("Unable to access the Dynamic Logging provider, log level not changed");
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
            _logger?.LogError("Exception deserializing LoggersEndpoint Request: {Exception}", e);
        }

        return new Dictionary<string, string>();
    }
}
