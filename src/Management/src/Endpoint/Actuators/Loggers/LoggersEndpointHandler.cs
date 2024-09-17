// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Logging;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Loggers;

internal sealed class LoggersEndpointHandler : ILoggersEndpointHandler
{
    private static readonly ReadOnlyCollection<string> Levels = new List<string>
    {
        LoggerLevels.LogLevelToString(LogLevel.None),
        LoggerLevels.LogLevelToString(LogLevel.Critical),
        LoggerLevels.LogLevelToString(LogLevel.Error),
        LoggerLevels.LogLevelToString(LogLevel.Warning),
        LoggerLevels.LogLevelToString(LogLevel.Information),
        LoggerLevels.LogLevelToString(LogLevel.Debug),
        LoggerLevels.LogLevelToString(LogLevel.Trace)
    }.AsReadOnly();

    private readonly IOptionsMonitor<LoggersEndpointOptions> _optionsMonitor;
    private readonly IDynamicLoggerProvider _dynamicLoggerProvider;
    private readonly ILogger<LoggersEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public LoggersEndpointHandler(IOptionsMonitor<LoggersEndpointOptions> optionsMonitor, IDynamicLoggerProvider dynamicLoggerProvider,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(dynamicLoggerProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _dynamicLoggerProvider = dynamicLoggerProvider;
        _logger = loggerFactory.CreateLogger<LoggersEndpointHandler>();
    }

    public Task<LoggersResponse> InvokeAsync(LoggersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Invoke({Request})", SecurityUtilities.SanitizeInput(request.ToString()));

        Dictionary<string, object> logLevels;

        if (request.Type == LoggersRequestType.Get)
        {
            logLevels = GetLogLevels();
        }
        else
        {
            SetLogLevel(request.Name!, request.Level);
            logLevels = [];
        }

        var response = new LoggersResponse(logLevels, false);
        return Task.FromResult(response);
    }

    private Dictionary<string, object> GetLogLevels()
    {
        var result = new Dictionary<string, object>
        {
            { "levels", Levels }
        };

        ICollection<DynamicLoggerConfiguration> configurations = _dynamicLoggerProvider.GetLoggerConfigurations();
        var loggers = new Dictionary<string, LoggerLevels>();

        foreach (DynamicLoggerConfiguration configuration in configurations.OrderBy(entry => entry.CategoryName))
        {
            _logger.LogTrace("Adding {Configuration}", configuration);
            var levels = new LoggerLevels(configuration.ConfigurationMinLevel, configuration.EffectiveMinLevel);
            loggers.Add(configuration.CategoryName, levels);
        }

        result.Add("loggers", loggers);
        return result;
    }

    private void SetLogLevel(string name, string? level)
    {
        LogLevel? logLevel = LoggerLevels.StringToLogLevel(level);
        _dynamicLoggerProvider.SetLogLevel(name, logLevel);
    }
}
