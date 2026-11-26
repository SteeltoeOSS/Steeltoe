// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

internal sealed partial class LogFileEndpointHandler : ILogFileEndpointHandler
{
    internal const string FileProviderServiceKey = "Steeltoe.Management.Endpoint.LogFile";

    private static readonly char[] WildcardCharacters =
    [
        '*',
        '?'
    ];

    private readonly IOptionsMonitor<LogFileEndpointOptions> _optionsMonitor;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<LogFileEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public LogFileEndpointHandler(IOptionsMonitor<LogFileEndpointOptions> optionsMonitor,
        [FromKeyedServices(FileProviderServiceKey)] IFileProvider fileProvider, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(fileProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _fileProvider = fileProvider;
        _logger = loggerFactory.CreateLogger<LogFileEndpointHandler>();
    }

    public Task<LogFileResponse> InvokeAsync(LogFileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        LogResolvingFilePath();

        string? filePath = _optionsMonitor.CurrentValue.FilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            LogFilePathNotConfigured();
            return Task.FromResult(LogFileResponse.NotFound);
        }

        // Resolve a relative path from the base directory, which unlike the current directory doesn't depend on how the app was started.
        string fullPath = Path.GetFullPath(filePath, AppContext.BaseDirectory);

        IFileInfo? file = FindFile(fullPath);

        if (file == null)
        {
            // Some logging frameworks don't create the file until the first entry is written, so this is expected.
            LogFileNotFound(fullPath);
            return Task.FromResult(LogFileResponse.NotFound);
        }

        Stream stream;

        try
        {
            stream = file.CreateReadStream();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The logger may have rolled over or deleted the file in the meantime, or the file may not be accessible.
            LogFileNotReadable(file.PhysicalPath ?? fullPath, exception);
            return Task.FromResult(LogFileResponse.NotFound);
        }

        try
        {
            return Task.FromResult(CreateResponse(stream, request, file.PhysicalPath ?? fullPath));
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Finds the file at the specified path. When the file name contains wildcards, the matching file with the latest last modified time is returned.
    /// </summary>
    private IFileInfo? FindFile(string fullPath)
    {
        string fileName = Path.GetFileName(fullPath);
        string? directory = Path.GetDirectoryName(fullPath);

        if (fileName.IndexOfAny(WildcardCharacters) == -1 || directory == null)
        {
            IFileInfo file = _fileProvider.GetFileInfo(fullPath);
            return file is { Exists: true, IsDirectory: false } ? file : null;
        }

        IDirectoryContents directoryContents = _fileProvider.GetDirectoryContents(directory);

        if (!directoryContents.Exists)
        {
            return null;
        }

        // Matching is case-insensitive on platforms where the file system is.
        bool ignoreCase = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

        // Serilog inserts a sequence number into the name of each file it rolls to, so when timestamps are equal, the highest name is the newest.
        StringComparer nameComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        return directoryContents.Where(file => !file.IsDirectory && FileSystemName.MatchesSimpleExpression(fileName, file.Name, ignoreCase))
            .OrderByDescending(file => file.LastModified).ThenByDescending(file => file.Name, nameComparer).FirstOrDefault();
    }

    private LogFileResponse CreateResponse(Stream stream, LogFileRequest request, string filePath)
    {
        // The logger may append to the file while it is being sent. Only the bytes that exist now are sent, so the response matches its headers.
        var totalSize = (ulong)stream.Length;

        if (request.Ranges.Count == 0)
        {
            LogReturning(totalSize, 0, filePath);
            return new LogFileResponse(LogFileResponseStatus.Complete, 0, totalSize, totalSize, stream);
        }

        // Multipart responses are not supported. Rejecting the request is cheaper than sending the entire file, which can be large.
        if (request.Ranges.Count > 1 || !request.Ranges[0].TryGetByteRange(totalSize, out ulong start, out ulong end))
        {
            LogRangeNotSatisfiable(request, filePath, totalSize);
            stream.Dispose();
            return LogFileResponse.CreateNotSatisfiable(totalSize);
        }

        ulong length = end - start + 1;
        LogReturning(length, start, filePath);

        stream.Seek((long)start, SeekOrigin.Begin);
        return new LogFileResponse(LogFileResponseStatus.Partial, start, length, totalSize, stream);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Resolving the path to the log file.")]
    private partial void LogResolvingFilePath();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The log file actuator is enabled, but no log file is configured. Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.")]
    private partial void LogFilePathNotConfigured();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Log file '{FilePath}' does not exist.")]
    private partial void LogFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Log file '{FilePath}' could not be opened.")]
    private partial void LogFileNotReadable(string filePath, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Range '{Range}' is not satisfiable for log file '{FilePath}' with length {FileLength}.")]
    private partial void LogRangeNotSatisfiable(LogFileRequest range, string filePath, ulong fileLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning {Length} bytes at offset {Offset} from log file '{FilePath}'.")]
    private partial void LogReturning(ulong length, ulong offset, string filePath);
}
