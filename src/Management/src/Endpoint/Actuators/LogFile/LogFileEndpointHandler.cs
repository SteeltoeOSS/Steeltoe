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

    private static readonly char[] WildcardCharacters = ['*', '?'];

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

    public Task<LogFileResult> InvokeAsync(LogFileRequest? request, CancellationToken cancellationToken)
    {
        LogResolvingFilePath();

        string? filePath = _optionsMonitor.CurrentValue.FilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            LogFilePathNotConfigured();
            return Task.FromResult(LogFileResult.NotFound);
        }

        // Resolve a relative path from the base directory, which unlike the current directory doesn't depend on how the app was started.
        string fullPath = Path.GetFullPath(filePath, AppContext.BaseDirectory);

        IFileInfo? file = FindFile(fullPath);

        if (file == null)
        {
            // Some logging frameworks don't create the file until the first entry is written, so this is expected.
            LogFileNotFound(fullPath);
            return Task.FromResult(LogFileResult.NotFound);
        }

        Stream stream;

        try
        {
            stream = file.CreateReadStream();
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            // The logger may have rolled over or deleted the file in the meantime.
            LogFileNotFound(file.PhysicalPath ?? fullPath);
            return Task.FromResult(LogFileResult.NotFound);
        }

        try
        {
            return Task.FromResult(CreateResult(stream, request ?? new LogFileRequest(), file.PhysicalPath ?? fullPath));
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
        return directoryContents.Where(file => !file.IsDirectory && FileSystemName.MatchesSimpleExpression(fileName, file.Name, ignoreCase))
            .OrderByDescending(file => file.LastModified).ThenByDescending(file => file.Name, StringComparer.Ordinal).FirstOrDefault();
    }

    private LogFileResult CreateResult(Stream stream, LogFileRequest request, string filePath)
    {
        // The logger may append to the file while it is being sent. Only the bytes that exist now are sent, so the response matches its headers.
        long totalSize = stream.Length;

        if (!request.HasRange)
        {
            LogReturning(totalSize, 0, filePath);
            return new LogFileResult(LogFileResultStatus.Complete, 0, totalSize, totalSize, stream);
        }

        if (!TryGetByteRange(request, totalSize, out long start, out long end))
        {
            LogRangeNotSatisfiable(request, filePath, totalSize);
            stream.Dispose();
            return LogFileResult.CreateNotSatisfiable(totalSize);
        }

        long length = end - start + 1;
        LogReturning(length, start, filePath);

        stream.Seek(start, SeekOrigin.Begin);
        return new LogFileResult(LogFileResultStatus.Partial, start, length, totalSize, stream);
    }

    private static bool TryGetByteRange(LogFileRequest request, long totalSize, out long start, out long end)
    {
        start = 0;
        end = 0;

        if (request.HasMultipleRanges)
        {
            // Multipart responses are not supported. Rejecting the request is cheaper than sending the entire file, which can be large.
            return false;
        }

        if (request.RangeStart == null)
        {
            // Suffix range, such as "bytes=-500", which requests the last 500 bytes.
            long suffixLength = request.RangeEnd.GetValueOrDefault();

            if (suffixLength <= 0 || totalSize == 0)
            {
                return false;
            }

            start = Math.Max(totalSize - suffixLength, 0);
            end = totalSize - 1;
            return true;
        }

        if (request.RangeStart < 0 || request.RangeStart >= totalSize || request.RangeEnd < request.RangeStart)
        {
            return false;
        }

        start = request.RangeStart.Value;
        end = request.RangeEnd == null ? totalSize - 1 : Math.Min(request.RangeEnd.Value, totalSize - 1);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Resolving the path to the log file.")]
    private partial void LogResolvingFilePath();

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "The log file actuator is enabled, but no log file is configured. Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.")]
    private partial void LogFilePathNotConfigured();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Log file '{FilePath}' does not exist.")]
    private partial void LogFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Range '{Range}' is not satisfiable for log file '{FilePath}' with length {FileLength}.")]
    private partial void LogRangeNotSatisfiable(LogFileRequest range, string filePath, long fileLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning {Length} bytes at offset {Offset} from log file '{FilePath}'.")]
    private partial void LogReturning(long length, long offset, string filePath);
}
