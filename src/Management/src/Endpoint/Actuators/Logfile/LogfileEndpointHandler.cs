// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Logfile;

public sealed class LogfileEndpointHandler : ILogfileEndpointHandler
{
    private readonly IOptionsMonitor<LogfileEndpointOptions> _optionsMonitor;
    private readonly ILogger<LogfileEndpointHandler> _logger;

    public LogfileEndpointHandler(IOptionsMonitor<LogfileEndpointOptions> optionsMonitorMonitor, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitorMonitor);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitorMonitor;
        _logger = loggerFactory.CreateLogger<LogfileEndpointHandler>();
    }

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public async Task<string> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking LogfileEndpointHandler with argument: {Argument}", argument);
        cancellationToken.ThrowIfCancellationRequested();

        string logFilePath = GetLogFilePath();

        if (!string.IsNullOrEmpty(logFilePath))
        {
            return await File.ReadAllTextAsync(logFilePath, cancellationToken: cancellationToken);
        }

        _logger.LogWarning("Log file path is not set");
        return string.Empty;

    }

    internal string GetLogFilePath()
    {
        _logger.LogTrace("Getting log file path");

        if (!string.IsNullOrEmpty(_optionsMonitor.CurrentValue.FilePath))
        {
            string entryAssemblyDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            return Path.Combine(entryAssemblyDirectory, _optionsMonitor.CurrentValue.FilePath ?? string.Empty);
        }

        _logger.LogWarning("File path is not set");
        return string.Empty;
    }
}
