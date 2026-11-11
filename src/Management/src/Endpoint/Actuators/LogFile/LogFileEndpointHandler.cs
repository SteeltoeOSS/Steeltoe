// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal sealed partial class LogFileEndpointHandler : ILogFileEndpointHandler
{
    private readonly IOptionsMonitor<LogFileEndpointOptions> _optionsMonitor;
    private readonly ILogger<LogFileEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public LogFileEndpointHandler(IOptionsMonitor<LogFileEndpointOptions> optionsMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _logger = loggerFactory.CreateLogger<LogFileEndpointHandler>();
    }

    public Task<string?> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        LogResolvingFilePath();

        string? filePath = _optionsMonitor.CurrentValue.FilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            LogFilePathNotConfigured();
            return Task.FromResult<string?>(null);
        }

        // Like log4net and NLog, resolve a relative path from the application directory, because the current directory isn't stable.
        string fullPath = Path.GetFullPath(filePath, AppContext.BaseDirectory);
        return Task.FromResult<string?>(fullPath);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Resolving the path to the log file.")]
    private partial void LogResolvingFilePath();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The log file actuator is enabled, but no log file is configured. Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.")]
    private partial void LogFilePathNotConfigured();
}
