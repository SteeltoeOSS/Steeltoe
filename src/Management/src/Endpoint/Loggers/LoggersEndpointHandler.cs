// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
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

    private static readonly IDictionary<string, object> EmptyDictionary = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

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

    public Task<LoggersResponse> InvokeAsync(LoggersRequest request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        _logger.LogDebug("Invoke({request})", SecurityUtilities.SanitizeInput(request.ToString()));

        IDictionary<string, object> result;

        if (request.Type == LoggersRequestType.Get)
        {
            result = GetLogLevels();
        }
        else
        {
            SetLogLevel(request.Name, request.Level);
            result = EmptyDictionary;
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

        ICollection<ILoggerConfiguration> configurations = _dynamicLoggerProvider.GetLoggerConfigurations();
        var loggers = new Dictionary<string, LoggerLevels>();

        foreach (ILoggerConfiguration configuration in configurations.OrderBy(entry => entry.Name))
        {
            _logger.LogTrace("Adding {configuration}", configuration);
            var levels = new LoggerLevels(configuration.ConfiguredLevel, configuration.EffectiveLevel);
            loggers.Add(configuration.Name, levels);
        }

        result.Add("loggers", new ReadOnlyDictionary<string, LoggerLevels>(loggers));
        return new ReadOnlyDictionary<string, object>(result);
    }

    private void SetLogLevel(string name, string level)
    {
        LogLevel? logLevel = LoggerLevels.StringToLogLevel(level);
        _dynamicLoggerProvider.SetLogLevel(name, logLevel);
    }
}
