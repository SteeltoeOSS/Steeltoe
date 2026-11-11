// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.LogFile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.LogFile;

public sealed class LogFileActuatorTest
{
    private const string LogFileContent = "0123456789";

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
        using var logFile = new TempFile();
        byte[] fileContent = Encoding.UTF8.GetBytes("Hello, wörld! 👋\n");
        await File.WriteAllBytesAsync(logFile.FullPath, fileContent, TestContext.Current.CancellationToken);

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(GetAppSettings(logFile.FullPath)));
            builder.ConfigureServices(services => services.AddLogFileActuator());
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/logfile"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.AcceptRanges.Should().ContainSingle().Which.Should().Be("bytes");

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("text/plain");
        response.Content.Headers.ContentLength.Should().Be(fileContent.Length);
        response.Content.Headers.ContentRange.Should().BeNull();

        byte[] responseBody = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Equal(fileContent);
    }

    [Theory]
    [InlineData("bytes=0-3", "0123", "bytes 0-3/10")]
    [InlineData("bytes=6-", "6789", "bytes 6-9/10")]
    [InlineData("bytes=-4", "6789", "bytes 6-9/10")]
    [InlineData("bytes=8-100", "89", "bytes 8-9/10")]
    [InlineData("bytes=-100", "0123456789", "bytes 0-9/10")]
    [InlineData("bytes=9-9", "9", "bytes 9-9/10")]
    [InlineData("BYTES=2-4", "234", "bytes 2-4/10")]
    public async Task Endpoint_returns_requested_byte_range(string rangeHeader, string expectedContent, string expectedContentRange)
    {
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, LogFileContent, TestContext.Current.CancellationToken);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("text/plain");
        response.Content.Headers.ContentLength.Should().Be(expectedContent.Length);
        response.Content.Headers.ContentRange.Should().NotBeNull();
        response.Content.Headers.ContentRange.ToString().Should().Be(expectedContentRange);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be(expectedContent);
    }

    [Theory]
    [InlineData("bytes=10-")]
    [InlineData("bytes=100-200")]
    [InlineData("bytes=-0")]
    [InlineData("bytes=0-1, 4-5")]
    public async Task Endpoint_rejects_unsatisfiable_byte_range(string rangeHeader)
    {
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, LogFileContent, TestContext.Current.CancellationToken);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentRange.Should().NotBeNull();
        response.Content.Headers.ContentRange.ToString().Should().Be("bytes */10");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.OK)]
    [InlineData("bytes=0-", HttpStatusCode.RequestedRangeNotSatisfiable)]
    [InlineData("bytes=-300", HttpStatusCode.RequestedRangeNotSatisfiable)]
    public async Task Endpoint_handles_empty_log_file(string? rangeHeader, HttpStatusCode expectedStatusCode)
    {
        using var logFile = new TempFile();

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(expectedStatusCode);

        if (expectedStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            response.Content.Headers.ContentRange.Should().NotBeNull();
            response.Content.Headers.ContentRange.ToString().Should().Be("bytes */0");
        }

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().BeEmpty();
    }

    [Theory]
    [InlineData("lines=0-3")]
    [InlineData("bytes=invalid")]
    [InlineData("bytes=5-2")]
    public async Task Endpoint_ignores_unsupported_or_invalid_range_header(string rangeHeader)
    {
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, LogFileContent, TestContext.Current.CancellationToken);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, rangeHeader);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentRange.Should().BeNull();

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be(LogFileContent);
    }

    [Fact]
    public async Task Endpoint_supports_tailing_by_Spring_Boot_Admin()
    {
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, "line 1\nline 2\n", TestContext.Current.CancellationToken);

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
        using HttpClient httpClient = host.GetTestClient();

        // Spring Boot Admin first requests the tail of the file. It then polls from the last byte of the last complete line it has seen, and starts
        // over when it receives HTTP 416 with a different total length, which indicates that the file was rolled over.
        using (HttpResponseMessage response = await SendRequestAsync(httpClient, "bytes=-307200"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
            response.Content.Headers.ContentRange.Should().NotBeNull();
            response.Content.Headers.ContentRange.ToString().Should().Be("bytes 0-13/14");

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("line 1\nline 2\n");
        }

        using (HttpResponseMessage response = await SendRequestAsync(httpClient, "bytes=13-"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
            response.Content.Headers.ContentRange.Should().NotBeNull();
            response.Content.Headers.ContentRange.ToString().Should().Be("bytes 13-13/14");

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("\n");
        }

        await File.AppendAllTextAsync(logFile.FullPath, "line 3\n", TestContext.Current.CancellationToken);

        using (HttpResponseMessage response = await SendRequestAsync(httpClient, "bytes=13-"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
            response.Content.Headers.ContentRange.Should().NotBeNull();
            response.Content.Headers.ContentRange.ToString().Should().Be("bytes 13-20/21");

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("\nline 3\n");
        }

        await File.WriteAllTextAsync(logFile.FullPath, "new\n", TestContext.Current.CancellationToken);

        using (HttpResponseMessage response = await SendRequestAsync(httpClient, "bytes=20-"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
            response.Content.Headers.ContentRange.Should().NotBeNull();
            response.Content.Headers.ContentRange.ToString().Should().Be("bytes */4");
        }
    }

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

        using (HttpResponseMessage response = await SendRequestAsync(httpClient, null))
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("first entry\n");
        }

        await writer.WriteAsync("second entry\n");
        await writer.FlushAsync(TestContext.Current.CancellationToken);

        using (HttpResponseMessage response = await SendRequestAsync(httpClient, "bytes=12-"))
        {
            response.StatusCode.Should().Be(HttpStatusCode.PartialContent);

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be("second entry\n");
        }
    }

    [Fact]
    public async Task Endpoint_resolves_relative_path_from_application_directory()
    {
        string fileName = $"{Guid.NewGuid()}.log";
        string fullPath = Path.Combine(AppContext.BaseDirectory, fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, LogFileContent, TestContext.Current.CancellationToken);

            await using HostWrapper host = await StartHostAsync(GetAppSettings(fileName));
            using HttpClient httpClient = host.GetTestClient();

            using HttpResponseMessage response = await SendRequestAsync(httpClient, null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            responseBody.Should().Be(LogFileContent);
        }
        finally
        {
            File.Delete(fullPath);
        }
    }

    [Fact]
    public async Task Endpoint_returns_not_found_when_log_file_does_not_exist()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Warning);
        string filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing.log");

        await using HostWrapper host = await StartHostAsync(GetAppSettings(filePath), loggerProvider);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, null);

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

        await using HostWrapper host = await StartHostAsync(appSettings, loggerProvider);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await SendRequestAsync(httpClient, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().Be(
            $"WARN {typeof(LogFileEndpointHandler)}: The log file actuator is enabled, but no log file is configured. " +
            "Set 'Management:Endpoints:LogFile:FilePath' to the path of the log file.");
    }

    [Fact]
    public async Task Endpoint_rejects_incompatible_accept_header()
    {
        using var logFile = new TempFile();

        await using HostWrapper host = await StartHostAsync(GetAppSettings(logFile.FullPath));
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
        using var logFile = new TempFile();
        await File.WriteAllTextAsync(logFile.FullPath, LogFileContent, TestContext.Current.CancellationToken);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*",
            ["Management:Endpoints:LogFile:FilePath"] = isFilePathConfigured ? logFile.FullPath : null
        };

        await using HostWrapper host = await StartHostAsync(appSettings, configureServices: services => services.AddHypermediaActuator());
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage logFileResponse = await SendRequestAsync(httpClient, null);
        logFileResponse.StatusCode.Should().Be(isFilePathConfigured ? HttpStatusCode.OK : HttpStatusCode.NotFound);

        using HttpResponseMessage linksResponse = await httpClient.GetAsync(new Uri("http://localhost/actuator"), TestContext.Current.CancellationToken);
        linksResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string linksResponseBody = await linksResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonNode? linksNode = JsonNode.Parse(linksResponseBody)?["_links"];

        linksNode.Should().NotBeNull();
        linksNode.AsObject().ContainsKey("logfile").Should().Be(isFilePathConfigured);
    }

    private static Dictionary<string, string?> GetAppSettings(string filePath)
    {
        return new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "logfile",
            ["Management:Endpoints:LogFile:FilePath"] = filePath
        };
    }

    private static async Task<HostWrapper> StartHostAsync(Dictionary<string, string?> appSettings, ILoggerProvider? loggerProvider = null,
        Action<IServiceCollection>? configureServices = null)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);

        if (loggerProvider != null)
        {
            builder.Logging.AddProvider(loggerProvider);
        }

        builder.Services.AddLogFileActuator();
        configureServices?.Invoke(builder.Services);

        HostWrapper host = HostWrapper.Wrap(builder.Build());
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static async Task<HttpResponseMessage> SendRequestAsync(HttpClient httpClient, string? rangeHeader)
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
