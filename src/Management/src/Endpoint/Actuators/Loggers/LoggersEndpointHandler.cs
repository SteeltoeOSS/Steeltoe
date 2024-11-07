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
    private const string SpringDefaultCategoryName = "Default";

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

        ICollection<DynamicLoggerState> loggerStates = _dynamicLoggerProvider.GetLogLevels();
        var loggerLevelsPerCategory = new Dictionary<string, LoggerLevels>();

        foreach (DynamicLoggerState loggerState in loggerStates.OrderBy(entry => entry.CategoryName))
        {
            _logger.LogTrace("Adding {LoggerState}", loggerState);

            string categoryName = loggerState.CategoryName.Length == 0 ? SpringDefaultCategoryName : loggerState.CategoryName;
            var levels = new LoggerLevels(loggerState.BackupMinLevel, loggerState.EffectiveMinLevel);
            loggerLevelsPerCategory.Add(categoryName, levels);
        }

        result.Add("loggers", loggerLevelsPerCategory);
        return result;
    }

    private void SetLogLevel(string name, string? level)
    {
        string categoryName = name == SpringDefaultCategoryName ? string.Empty : name;
        LogLevel? logLevel = LoggerLevels.StringToLogLevel(level);

        _dynamicLoggerProvider.SetLogLevel(categoryName, logLevel);
    }
}
