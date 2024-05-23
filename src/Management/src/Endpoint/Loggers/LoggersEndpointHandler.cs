// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Logging;

namespace Steeltoe.Management.Endpoint.Loggers;

internal sealed class LoggersEndpointHandler : ILoggersEndpointHandler
{
    private static readonly IList<string> Levels = new List<string>
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
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(dynamicLoggerProvider);
        ArgumentGuard.NotNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _dynamicLoggerProvider = dynamicLoggerProvider;
        _logger = loggerFactory.CreateLogger<LoggersEndpointHandler>();
    }

    public Task<LoggersResponse> InvokeAsync(LoggersRequest request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        _logger.LogDebug("Invoke({Request})", SecurityUtilities.SanitizeInput(request.ToString()));

        IDictionary<string, object> result;

        if (request.Type == LoggersRequestType.Get)
        {
            result = GetLogLevels();
        }
        else
        {
            SetLogLevel(request.Name!, request.Level);
            result = new Dictionary<string, object>();
        }

        var response = new LoggersResponse(result, false);
        return Task.FromResult(response);
    }

    private IDictionary<string, object> GetLogLevels()
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
