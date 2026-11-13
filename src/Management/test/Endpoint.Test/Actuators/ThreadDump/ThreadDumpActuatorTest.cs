// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class ThreadDumpActuatorTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "threaddump"
    };

    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddThreadDumpActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => serviceProvider.GetRequiredService<ThreadDumpEndpointMiddleware>();

        action.Should().NotThrow();
    }

    [Fact]
    public async Task Configures_default_settings()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddThreadDumpActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ThreadDumpEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ThreadDumpEndpointOptions>>().Value;

        options.Duration.Should().Be(10);
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("threaddump");
        options.Path.Should().Be("threaddump");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("GET");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/actuators").Should().Be("/actuators/threaddump");
    }

    [Fact]
    public async Task Configures_custom_settings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:ThreadDump:Duration"] = "20",
            ["Management:Endpoints:ThreadDump:Enabled"] = "true",
            ["Management:Endpoints:ThreadDump:Id"] = "test-actuator-id",
            ["Management:Endpoints:ThreadDump:Path"] = "test-actuator-path",
            ["Management:Endpoints:ThreadDump:RequiredPermissions"] = "full",
            ["Management:Endpoints:ThreadDump:AllowedVerbs:0"] = "post"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddThreadDumpActuator();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        ThreadDumpEndpointOptions options = serviceProvider.GetRequiredService<IOptions<ThreadDumpEndpointOptions>>().Value;

        options.Duration.Should().Be(20);
        options.Enabled.Should().BeTrue();
        options.Id.Should().Be("test-actuator-id");
        options.Path.Should().Be("test-actuator-path");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Full);

        options.GetSafeAllowedVerbs().Should().ContainSingle().Subject.Should().Be("POST");
        options.RequiresExactMatch().Should().BeTrue();
        options.GetPathMatchPattern("/alt-actuators").Should().Be("/alt-actuators/test-actuator-path");
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_data(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IThreadDumper, FakeThreadDumper>();
                services.AddThreadDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/threaddump"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("application/vnd.spring-boot.actuator.v3+json");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().BeJson("""
            {
              "threads": [
                {
                  "stackTrace": [
                    {
                      "moduleName": "FakeAssembly",
                      "className": "FakeNamespace.FakeClass",
                      "methodName": "FakeMethod",
                      "fileName": "C:\\Source\\Code\\FakeClass.cs",
                      "lineNumber": 8,
                      "columnNumber": 16,
                      "nativeMethod": false
                    }
                  ],
                  "threadId": 18,
                  "threadName": "Thread-00018",
                  "threadState": "WAITING",
                  "inNative": false
                }
              ]
            }
            """);
    }

    [Theory]
    [InlineData(HostBuilderType.Host)]
    [InlineData(HostBuilderType.WebHost)]
    [InlineData(HostBuilderType.WebApplication)]
    public async Task Endpoint_returns_expected_plain_text(HostBuilderType hostBuilderType)
    {
        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IThreadDumper, FakeThreadDumper>();
                services.AddThreadDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/threaddump"));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/plain"));
        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.ToString().Should().Be("text/plain; charset=utf-8");

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        responseBody.Should().Be("""
            "Thread-00018" #18
               Thread State: WAITING
              at FakeNamespace.FakeClass.FakeMethod(C:\Source\Code\FakeClass.cs:8)

              Locked ownable synchronizers:
              - None


            """.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData("*/*", "application/vnd.spring-boot.actuator.v3+json")]
    [InlineData("text/*", "text/plain")]
    [InlineData("TEXT/PLAIN", "text/plain")]
    [InlineData("text/plain; charset=utf-8", "text/plain")]
    [InlineData("application/json, text/plain", "application/vnd.spring-boot.actuator.v3+json")]
    [InlineData("text/plain, application/json", "text/plain")]
    [InlineData("text/plain; q=0.5, application/json", "application/vnd.spring-boot.actuator.v3+json")]
    [InlineData("application/json; q=0.5, text/plain", "text/plain")]
    [InlineData("text/html, text/plain; q=0.9, */*; q=0.8", "text/plain")]
    public async Task Endpoint_negotiates_response_content_type(string acceptHeader, string expectedContentType)
    {
        await using HostWrapper host = HostBuilderType.WebApplication.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IThreadDumper, FakeThreadDumper>();
                services.AddThreadDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/threaddump"));
        request.Headers.TryAddWithoutValidation("Accept", acceptHeader).Should().BeTrue();
        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType.MediaType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task Endpoint_rejects_incompatible_accept_header()
    {
        await using HostWrapper host = HostBuilderType.WebApplication.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(AppSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IThreadDumper, FakeThreadDumper>();
                services.AddThreadDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/threaddump"));
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/html"));
        using HttpResponseMessage response = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);

        string responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseBody.Should().Be("Only the 'application/vnd.spring-boot.actuator.v3+json', 'text/plain' content types are supported.");
    }
}
