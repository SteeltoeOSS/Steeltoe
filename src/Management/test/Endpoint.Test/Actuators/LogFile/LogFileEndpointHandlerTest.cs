// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.LogFile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.LogFile;

public sealed class LogFileEndpointHandlerTest
{
    private const string LogFileContent = "0123456789";

    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");
    private static readonly DateTimeOffset BaseTime = 1.January(2026).At(12, 0).AsUtc();

    [Fact]
    public async Task Returns_entire_file_when_no_range_is_requested()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, "Hello, wörld! 👋\n");
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.Complete);
        result.Offset.Should().Be(0);
        result.Length.Should().Be((ulong)Encoding.UTF8.GetByteCount("Hello, wörld! 👋\n"));
        result.TotalSize.Should().Be(result.Length);

        string content = await ReadContentAsync(result);
        content.Should().Be("Hello, wörld! 👋\n");
    }

    [Fact]
    public async Task Throws_for_missing_request()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, LogFileContent);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        Func<Task> action = async () => await handler.InvokeAsync(null!, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0UL, 3UL, "0123", 0UL)]
    [InlineData(6UL, null, "6789", 6UL)]
    [InlineData(null, 4UL, "6789", 6UL)]
    [InlineData(8UL, 100UL, "89", 8UL)]
    [InlineData(null, 100UL, "0123456789", 0UL)]
    [InlineData(9UL, 9UL, "9", 9UL)]
    [InlineData(0UL, 9UL, "0123456789", 0UL)]
    public async Task Returns_requested_byte_range(ulong? rangeStart, ulong? rangeEnd, string expectedContent, ulong expectedOffset)
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, LogFileContent);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        var request = new LogFileRequest
        {
            Ranges = [new LogRange(rangeStart, rangeEnd)]
        };

        using LogFileResponse result = await handler.InvokeAsync(request, TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.Partial);
        result.Offset.Should().Be(expectedOffset);
        result.Length.Should().Be((ulong)expectedContent.Length);
        result.TotalSize.Should().Be(10);

        string content = await ReadContentAsync(result);
        content.Should().Be(expectedContent);
    }

    [Theory]
    [InlineData(10UL, null)]
    [InlineData(100UL, 200UL)]
    [InlineData(null, 0UL)]
    [InlineData(5UL, 2UL)]
    public async Task Rejects_unsatisfiable_byte_range(ulong? rangeStart, ulong? rangeEnd)
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, LogFileContent);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        var request = new LogFileRequest
        {
            Ranges = [new LogRange(rangeStart, rangeEnd)]
        };

        using LogFileResponse result = await handler.InvokeAsync(request, TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.RangeNotSatisfiable);
        result.TotalSize.Should().Be(10);
        result.Content.Should().BeNull();
    }

    [Fact]
    public async Task Rejects_multiple_byte_ranges()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, LogFileContent);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        var request = new LogFileRequest
        {
            Ranges = [new LogRange(0, 1), new LogRange(4, 5)]
        };

        using LogFileResponse result = await handler.InvokeAsync(request, TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.RangeNotSatisfiable);
        result.TotalSize.Should().Be(10);
        result.Content.Should().BeNull();
    }

    [Theory]
    [InlineData(null, null, LogFileResponseStatus.Complete)]
    [InlineData(0UL, null, LogFileResponseStatus.RangeNotSatisfiable)]
    [InlineData(null, 300UL, LogFileResponseStatus.RangeNotSatisfiable)]
    public async Task Handles_empty_file(ulong? rangeStart, ulong? rangeEnd, LogFileResponseStatus expectedStatus)
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, string.Empty);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        var request = new LogFileRequest
        {
            Ranges = rangeStart == null && rangeEnd == null ? [] : [new LogRange(rangeStart, rangeEnd)]
        };

        using LogFileResponse result = await handler.InvokeAsync(request, TestContext.Current.CancellationToken);

        result.Status.Should().Be(expectedStatus);
        result.TotalSize.Should().Be(0);
        result.Length.Should().Be(0);
    }

    [Fact]
    public async Task Supports_tailing_by_Spring_Boot_Admin()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, "line 1\nline 2\n");
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        // Spring Boot Admin first requests the tail of the file. It then polls from the last byte of the last complete line it has seen, and starts
        // over when it receives a range that is not satisfiable with a different total length, which indicates that the file was rolled over.
        using (LogFileResponse result = await handler.InvokeAsync(new LogFileRequest
        {
            Ranges = [new LogRange(null, 307_200)]
        }, TestContext.Current.CancellationToken))
        {
            result.Status.Should().Be(LogFileResponseStatus.Partial);
            result.Offset.Should().Be(0);
            result.TotalSize.Should().Be(14);
            (await ReadContentAsync(result)).Should().Be("line 1\nline 2\n");
        }

        fileProvider.ReplaceFile(LogFilePath, "line 1\nline 2\nline 3\n");

        using (LogFileResponse result = await handler.InvokeAsync(new LogFileRequest
        {
            Ranges = [new LogRange(13, null)]
        }, TestContext.Current.CancellationToken))
        {
            result.Status.Should().Be(LogFileResponseStatus.Partial);
            result.Offset.Should().Be(13);
            result.TotalSize.Should().Be(21);
            (await ReadContentAsync(result)).Should().Be("\nline 3\n");
        }

        fileProvider.ReplaceFile(LogFilePath, "new\n");

        using (LogFileResponse result = await handler.InvokeAsync(new LogFileRequest
        {
            Ranges = [new LogRange(20, null)]
        }, TestContext.Current.CancellationToken))
        {
            result.Status.Should().Be(LogFileResponseStatus.RangeNotSatisfiable);
            result.TotalSize.Should().Be(4);
        }
    }

    [Fact]
    public async Task Resolves_relative_path_from_base_directory()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(AppContext.BaseDirectory, "logs", "app.log"), LogFileContent);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine("logs", "app.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.Complete);
        (await ReadContentAsync(result)).Should().Be(LogFileContent);
    }

    [Fact]
    public async Task Returns_not_found_without_logging_warning_when_file_does_not_exist()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, new MemoryFileProvider(), loggerFactory);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);
        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_not_found_when_path_is_a_directory()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory(LogFilePath);
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Returns_not_found_and_logs_warning_when_file_path_is_not_configured(string? filePath)
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        LogFileEndpointHandler handler = CreateHandler(filePath, new MemoryFileProvider(), loggerFactory);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().Be(
            $"WARN {typeof(LogFileEndpointHandler)}: The log file actuator is enabled, but no log file is configured. " +
            "Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.");
    }

    [Fact]
    public async Task Plain_path_does_not_select_other_files()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, "first", BaseTime);
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "second", BaseTime.AddHours(1));
        LogFileEndpointHandler handler = CreateHandler(LogFilePath, fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        (await ReadContentAsync(result)).Should().Be("first");
    }

    [Theory]
    [InlineData("app*.log")]
    [InlineData("app_00?.log")]
    [InlineData("*.log")]
    public async Task Wildcard_selects_file_with_latest_last_modified_time(string pattern)
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app.log"), "oldest", BaseTime);
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "newest", BaseTime.AddHours(2));
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_002.log"), "middle", BaseTime.AddHours(1));
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_003.txt"), "unrelated", BaseTime.AddHours(3));
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, pattern), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.Complete);
        (await ReadContentAsync(result)).Should().Be("newest");
    }

    [Fact]
    public async Task Wildcard_selects_highest_name_when_last_modified_times_are_equal()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "first", BaseTime);
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_002.log"), "second", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app_*.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        (await ReadContentAsync(result)).Should().Be("second");
    }

    [Fact]
    public async Task Wildcard_selects_newer_file_after_roll_over()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app.log"), "first", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app*.log"), fileProvider);

        using (LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken))
        {
            (await ReadContentAsync(result)).Should().Be("first");
        }

        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "second", BaseTime.AddMinutes(1));

        using (LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken))
        {
            (await ReadContentAsync(result)).Should().Be("second");
        }
    }

    [Fact]
    public async Task Wildcard_applies_range_to_selected_file()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app.log"), "0123456789", BaseTime);
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "abcdef", BaseTime.AddMinutes(1));
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app*.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest
        {
            Ranges = [new LogRange(null, 2)]
        }, TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.Partial);
        result.Offset.Should().Be(4);
        result.TotalSize.Should().Be(6);
        (await ReadContentAsync(result)).Should().Be("ef");
    }

    [Fact]
    public async Task Wildcard_ignores_directories()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeDirectory(Path.Combine(LogDirectory, "app_dir.log"));
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app.log"), "file", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app*.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        (await ReadContentAsync(result)).Should().Be("file");
    }

    [Fact]
    public async Task Wildcard_resolves_relative_path_from_base_directory()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(AppContext.BaseDirectory, "logs", "app_001.log"), "found", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine("logs", "app*.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        (await ReadContentAsync(result)).Should().Be("found");
    }

    [Fact]
    public async Task Wildcard_without_match_returns_not_found()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "other.txt"), "other", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app*.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);
    }

    [Fact]
    public async Task Wildcard_in_missing_directory_returns_not_found()
    {
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(LogDirectory, "app*.log"), new MemoryFileProvider());

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);
    }

    [Fact]
    public async Task Wildcard_in_directory_name_is_not_supported()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app.log"), "file", BaseTime);
        LogFileEndpointHandler handler = CreateHandler(Path.Combine(AppContext.BaseDirectory, "lo*", "app.log"), fileProvider);

        using LogFileResponse result = await handler.InvokeAsync(new LogFileRequest(), TestContext.Current.CancellationToken);

        result.Status.Should().Be(LogFileResponseStatus.NotFound);
    }

    private static LogFileEndpointHandler CreateHandler(string? filePath, MemoryFileProvider fileProvider, ILoggerFactory? loggerFactory = null)
    {
        var options = new LogFileEndpointOptions
        {
            FilePath = filePath
        };

        return new LogFileEndpointHandler(TestOptionsMonitor.Create(options), fileProvider, loggerFactory ?? NullLoggerFactory.Instance);
    }

    private static async Task<string> ReadContentAsync(LogFileResponse result)
    {
        result.Content.Should().NotBeNull();

        byte[] buffer = new byte[result.Length];
        await result.Content.ReadExactlyAsync(buffer, TestContext.Current.CancellationToken);
        return Encoding.UTF8.GetString(buffer);
    }
}
