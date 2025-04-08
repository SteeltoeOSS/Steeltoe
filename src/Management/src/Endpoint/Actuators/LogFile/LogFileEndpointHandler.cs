// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Logfile;

internal sealed class LogFileEndpointHandler : ILogFileEndpointHandler
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

    public async Task<string> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking LogfileEndpointHandler with argument: {Argument}", argument);
        cancellationToken.ThrowIfCancellationRequested();

        string logFilePath = GetLogFilePath();

        if (!string.IsNullOrEmpty(logFilePath))
        {
            return await File.ReadAllTextAsync(logFilePath, cancellationToken);
        }

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
