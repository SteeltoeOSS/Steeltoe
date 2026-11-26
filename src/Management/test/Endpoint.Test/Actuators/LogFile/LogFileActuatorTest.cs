// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.LogFile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.LogFile;

public sealed class LogFileActuatorTest
{
    private const string LogFileContent = "0123456789";

    private static readonly string LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "app.log");

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddLogFileActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<LogFileEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddLogFileActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        LogFileEndpointOptions options = serviceProvider.GetRequiredService<IOptions<LogFileEndpointOptions>>().Value;

        options.FilePath.Should().BeNull();
        options.Enabled.Should().BeFalse();
        options.Id.Should().Be("logfile");
        options.Path.Should().Be("logfile");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/logfile");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:LogFile:FilePath"] = "logs/app.log",
            ["Management:Endpoints:LogFile:Enabled"] = "true",
            ["Management:Endpoints:LogFile:Id"] = "test-actuator-id",
            ["Management:Endpoints:LogFile:Path"] = "test-actuator-path",
            ["Management:Endpoints:LogFile:RequiredPermissions"] = "full",
            ["Management:Endpoints:LogFile:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogFileActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        LogFileEndpointOptions options = serviceProvider.GetRequiredService<IOptions<LogFileEndpointOptions>>().Value;

        options.FilePath.Should().Be("logs/app.log");
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Fact]
    public async Task Does_not_turn_off_endpoint_when_file_path_is_configured()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:LogFile:FilePath"] = "logs/app.log"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddLogFileActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        LogFileEndpointOptions options = serviceProvider.GetRequiredService<IOptions<LogFileEndpointOptions>>().Value;

        options.Enabled.Should().BeNull();
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_entire_log_file(HostBuilderType hostBuilderType)
    {
        const string fileContent = "Hello, wörld! 👋\n";
        byte[] fileBytes = Encoding.UTF8.GetBytes(fileContent);

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, fileBytes);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(GetAppSettings(LogFilePath)));

            builder.ConfigureServices(services =>
            {
                services.AddKeyedSingleton<IFileProvider>(LogFileEndpointHandler.FileProviderServiceKey, fileProvider);
                services.AddLogFileActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/logfile"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.AcceptRanges.Should().ContainSingle().Which.Should().Be("bytes");

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("text/plain");
        response.Content.Headers.ContentLength.Should().Be(fileBytes.Length);
        response.Content.Headers.ContentRange.Should().BeNull();

        byte[] responseBody = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Equal(fileBytes);
    }

    [Theory]
    [InlineData("bytes=0-3", "0123", "bytes 0-3/10")]
    [InlineData("bytes=6-", "6789", "bytes 6-9/10")]
    [InlineData("bytes=-4", "6789", "bytes 6-9/10")]
    [InlineData("bytes=8-100", "89", "bytes 8-9/10")]
    [InlineData("BYTES=2-4", "234", "bytes 2-4/10")]
    public async Task Endpoint_returns_requested_byte_range(string rangeHeader, string expectedContent, string expectedContentRange)
    {
        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), CreateFileProvider(LogFileContent));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Headers.AcceptRanges.Should().ContainSingle().Which.Should().Be("bytes");
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("text/plain");
        response.Content.Headers.ContentLength.Should().Be(expectedContent.Length);
        response.Content.Headers.ContentRange.Should().NotBeNull();
        response.Content.Headers.ContentRange.ToString().Should().Be(expectedContentRange);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be(expectedContent);
    }

    [Theory]
    [InlineData("bytes=10-", LogFileContent, "bytes */10")]
    [InlineData("bytes=-0", LogFileContent, "bytes */10")]
    [InlineData("bytes=0-1, 4-5", LogFileContent, "bytes */10")]
    [InlineData("bytes=0-", "", "bytes */0")]
    public async Task Endpoint_rejects_unsatisfiable_byte_range(string rangeHeader, string fileContent, string expectedContentRange)
    {
        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), CreateFileProvider(fileContent));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentRange.Should().NotBeNull();
        response.Content.Headers.ContentRange.ToString().Should().Be(expectedContentRange);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Fact]
    public async Task Endpoint_returns_empty_response_for_empty_log_file()
    {
        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), CreateFileProvider(string.Empty));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentLength.Should().Be(0);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Theory]
    [InlineData("lines=0-3")]
    [InlineData("bytes=invalid")]
    [InlineData("bytes=5-2")]
    public async Task Endpoint_ignores_unsupported_or_invalid_range_header(string rangeHeader)
    {
        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), CreateFileProvider(LogFileContent));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentRange.Should().BeNull();

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be(LogFileContent);
    }

    [Fact]
    public async Task Endpoint_returns_newest_file_matching_wildcard()
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, "old", 1.January(2026).At(12, 0).AsUtc());
        fileProvider.IncludeFile(Path.Combine(LogDirectory, "app_001.log"), "new", 1.January(2026).At(13, 0).AsUtc());

        await using HostWrapper host = await StartHostAsync(GetAppSettings(Path.Combine(LogDirectory, "app*.log")), fileProvider);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("new");
    }

    [Fact]
    public async Task Endpoint_returns_not_found_when_log_file_does_not_exist()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), new MemoryFileProvider(), loggerProvider);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Endpoint_returns_not_found_and_logs_warning_when_enabled_without_file_path()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "logfile",
            ["Management:Endpoints:LogFile:Enabled"] = "true"
        };

        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);

        await using HostWrapper host = await StartHostAsync(appSettings, new MemoryFileProvider(), loggerProvider);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().Be(
            $"WARN {typeof(LogFileEndpointHandler)}: The log file actuator is enabled, but no log file is configured. " +
            "Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.");
    }

    [Fact]
    public async Task Endpoint_rejects_incompatible_accept_header()
    {
        await using HostWrapper host = await StartHostAsync(GetAppSettings(LogFilePath), CreateFileProvider(LogFileContent));
        using HttpClient httpClient = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/logfile"));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Endpoint_is_only_available_when_file_path_is_configured(bool isFilePathConfigured)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
            ["Management:Endpoints:LogFile:FilePath"] = isFilePathConfigured ? LogFilePath : null
        };

        await using HostWrapper host = await StartHostAsync(appSettings, CreateFileProvider(LogFileContent),
            configureServices: services => services.AddHypermediaActuator());

        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage logFileResponse = await SendGetRequestAsync(httpClient, null);
        logFileResponse.StatusCode.Should().Be(isFilePathConfigured ? HttpStatusCode.OK : HttpStatusCode.NotFound);

        using HttpResponseMessage linksResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);
        linksResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string linksResponseBody = await linksResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonNode? linksNode = JsonNode.Parse(linksResponseBody)?["_links"];

        linksNode.Should().NotBeNull();
        linksNode.AsObject().ContainsKey("logfile").Should().Be(isFilePathConfigured);
    }

    // The following tests use the real file system, because file sharing between the endpoint and the logger can't be faked.

    [Fact]
    public async Task Endpoint_can_read_log_file_while_it_is_open_for_writing()
    {
        using var logFile = new TempFile();

        await using var writeStream = new FileStream(logFile.FullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(writeStream);
        await writer.WriteAsync("first entry\n");
        await writer.FlushAsync(TestContext.Current.CancellationToken);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        using (HttpResponseMessage response = await SendGetRequestAsync(httpClient, null))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("first entry\n");
        }

        await writer.WriteAsync("second entry\n");
        await writer.FlushAsync(TestContext.Current.CancellationToken);

        using (HttpResponseMessage response = await SendGetRequestAsync(httpClient, "bytes=12-"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.PartialContent);

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("second entry\n");
        }
    }

    [Fact]
    public async Task Endpoint_does_not_prevent_logger_from_deleting_log_file_while_it_is_being_read()
    {
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, LogFileContent, TestContext.Current.CancellationToken);

        var fileProvider = new PhysicalLogFileProvider();
        await using Stream stream = fileProvider.GetFileInfo(logFile.FullPath).CreateReadStream();

        // On Windows, this throws an IOException when the file was opened without FileShare.Delete.
        File.Delete(logFile.FullPath);

        byte[] buffer = new byte[LogFileContent.Length];
        await stream.ReadExactlyAsync(buffer, TestContext.Current.CancellationToken);
        Encoding.UTF8.GetString(buffer).Should().Be(LogFileContent);
    }

    [Fact]
    public async Task Endpoint_returns_newest_file_after_Serilog_rolls_over_on_file_size_limit()
    {
        using var sandbox = new Sandbox();
        string logFilePath = Path.Combine(sandbox.FullPath, "app.log");

        WriteEntriesWithRollOver(logFilePath);

        string[] files = Directory.GetFiles(sandbox.FullPath, "app*.log");
        files.Length.Should().BeGreaterThan(1);

        string newestFileName = files.Select(path => new FileInfo(path)).OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal).First().Name;

        newestFileName.Should().NotBe("app.log");

        await using HostWrapper host = await StartHostAsync(GetAppSettings(Path.Combine(sandbox.FullPath, "app*.log")));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string newestFileContent = await File.ReadAllTextAsync(Path.Combine(sandbox.FullPath, newestFileName), TestContext.Current.CancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be(newestFileContent);
        responseBody.Should().Contain("Entry 19");
    }

    [Fact]
    public async Task Endpoint_returns_frozen_first_file_after_Serilog_rolls_over_when_path_has_no_wildcard()
    {
        using var sandbox = new Sandbox();
        string logFilePath = Path.Combine(sandbox.FullPath, "app.log");

        WriteEntriesWithRollOver(logFilePath);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFilePath));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendGetRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Contain("Entry 00");
        responseBody.Should().NotContain("Entry 19");
    }

    private static void WriteEntriesWithRollOver(string logFilePath)
    {
        // Each entry is 20 bytes, so Serilog rolls over to app_001.log, app_002.log, and so on, after every 5 entries.
        using Logger logger = new LoggerConfiguration().WriteTo.File(logFilePath, fileSizeLimitBytes: 100, rollOnFileSizeLimit: true,
            outputTemplate: "{Message}{NewLine}").CreateLogger();

        for (int index = 0; index < 20; index++)
        {
            logger.Information("Entry {Index:D2} 0123456789", index);
        }
    }

    private static Dictionary<string, string?> GetAppSettings(string filePath)
    {
        return new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "logfile",
            ["Management:Endpoints:LogFile:FilePath"] = filePath
        };
    }

    private static MemoryFileProvider CreateFileProvider(string content)
    {
        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeFile(LogFilePath, content);
        return fileProvider;
    }

    private static async Task<HostWrapper> StartHostAsync(Dictionary<string, string?> appSettings, IFileProvider? fileProvider = null,
        ILoggerProvider? loggerProvider = null, Action<IServiceCollection>? configureServices = null)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

        if (loggerProvider != null)
        {
            builder.Logging.AddProvider(loggerProvider);
        }

        if (fileProvider != null)
        {
            builder.Services.AddKeyedSingleton(LogFileEndpointHandler.FileProviderServiceKey, fileProvider);
        }

        builder.Services.AddLogFileActuator();
        configureServices?.Invoke(builder.Services);

        HostWrapper host = HostWrapper.Wrap(builder.Build());
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static async Task<HttpResponseMessage> SendGetRequestAsync(HttpClient httpClient, string? rangeHeader)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/logfile"));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));

        if (rangeHeader != null)
        {
            request.Headers.TryAddWithoutValidation("Range", rangeHeader).Should().BeTrue();
        }

        return await httpClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
