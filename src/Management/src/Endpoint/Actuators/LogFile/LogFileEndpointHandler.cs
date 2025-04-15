// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

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

    public async Task<LogFileEndpointResponse> InvokeAsync(LogFileEndpointRequest? argument, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Invoking {Handler} with argument: {Argument}", nameof(LogFileEndpointHandler), argument);
        cancellationToken.ThrowIfCancellationRequested();

        string logFilePath = GetLogFilePath();

        if (!string.IsNullOrEmpty(logFilePath))
        {
            FileInfo logFile = new FileInfo(logFilePath);
            var logFileResult = new LogFileEndpointResponse(await File.ReadAllTextAsync(logFilePath, cancellationToken),
                logFile.Length,
                DetectFileEncoding(logFilePath),
                logFile.LastWriteTimeUtc);
            return logFileResult;
        }

        return new LogFileEndpointResponse();
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

    internal static Encoding DetectFileEncoding(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream, Encoding.Default);

        // Read the first 4 bytes (maximum BOM length)
        byte[] bom = reader.ReadBytes(4);

        // Check for known BOMs
        if (bom.Length >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
        {
            return Encoding.UTF8; // UTF-8 with BOM
        }
        if (bom.Length >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
        {
            return Encoding.Unicode; // UTF-16 LE
        }
        if (bom.Length >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode; // UTF-16 BE
        }
        if (bom is [0x00, 0x00, 0xFE, 0xFF, ..])
        {
            return Encoding.UTF32; // UTF-32 BE
        }
        if (bom is [0xFF, 0xFE, 0x00, 0x00, ..])
        {
            return Encoding.UTF32; // UTF-32 LE
        }

        // Default to UTF-8 if no BOM is found
        return Encoding.UTF8;
    }
}
