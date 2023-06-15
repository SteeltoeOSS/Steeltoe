// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Loggers;

internal sealed class LoggersEndpointHandler : ILoggersEndpointHandler
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

    private readonly ILogger<LoggersEndpointHandler> _logger;
    private readonly IOptionsMonitor<LoggersEndpointOptions> _options;
    private readonly IDynamicLoggerProvider _dynamicLoggerProvider;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public LoggersEndpointHandler(IOptionsMonitor<LoggersEndpointOptions> options, ILoggerFactory loggerFactory, IDynamicLoggerProvider dynamicLoggerProvider)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(dynamicLoggerProvider);

        _options = options;
        _dynamicLoggerProvider = dynamicLoggerProvider;
        _logger = loggerFactory.CreateLogger<LoggersEndpointHandler>();
    }

    public Task<Dictionary<string, object>> InvokeAsync(ILoggersRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Invoke({request})", SecurityUtilities.SanitizeInput(request?.ToString()));

        return Task.Run(() => DoInvoke(_dynamicLoggerProvider, request), cancellationToken);
    }

    private Dictionary<string, object> DoInvoke(IDynamicLoggerProvider provider, ILoggersRequest request)
    {
        var result = new Dictionary<string, object>();

        if (request is LoggersChangeRequest changeRequest)
        {
            SetLogLevel(provider, changeRequest.Name, changeRequest.Level);
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

    internal void AddLevels(Dictionary<string, object> result)
    {
        ArgumentGuard.NotNull(result);
        result.Add("levels", Levels);
    }

    internal ICollection<ILoggerConfiguration> GetLoggerConfigurations(IDynamicLoggerProvider provider)
    {
        if (provider == null)
        {
            _logger.LogInformation("Unable to access the Dynamic Logging provider, log configuration unavailable");
            return new List<ILoggerConfiguration>();
        }

        return provider.GetLoggerConfigurations();
    }

    internal void SetLogLevel(IDynamicLoggerProvider provider, string name, string level)
    {
        if (provider == null)
        {
            _logger.LogInformation("Unable to access the Dynamic Logging provider, log level not changed");
            return;
        }

        ArgumentGuard.NotNullOrEmpty(name);

        provider.SetLogLevel(name, LoggerLevels.MapLogLevel(level));
    }

    internal async Task<Dictionary<string, string>> DeserializeRequestAsync(Stream stream)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception deserializing loggers endpoint request.");
        }

        return new Dictionary<string, string>();
    }
}
