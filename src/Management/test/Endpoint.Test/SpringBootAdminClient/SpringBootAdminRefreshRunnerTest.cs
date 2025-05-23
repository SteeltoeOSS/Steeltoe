// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class SpringBootAdminRefreshRunnerTest
{
    private const string CurrentTime = "2021-03-31T23:57:53.896653Z";
    private static readonly string CurrentAppName = Assembly.GetEntryAssembly()!.GetName().Name!;

    [Fact]
    public async Task BindsConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://spring-boot-admin-server.com:9090",
            ["Spring:Boot:Admin:Client:ApplicationName"] = "test-app",
            ["Spring:Boot:Admin:Client:BaseUrl"] = "http://www.test-app.com:8080/api",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "http",
            ["Spring:Boot:Admin:Client:BaseHost"] = "www.test-app.com",
            ["Spring:Boot:Admin:Client:BasePort"] = "8080",
            ["Spring:Boot:Admin:Client:BasePath"] = "/api",
            ["Spring:Boot:Admin:Client:UseNetworkInterfaces"] = "true",
            ["Spring:Boot:Admin:Client:PreferIPAddress"] = "true",
            ["Spring:Boot:Admin:Client:ValidateCertificates"] = "false",
            ["Spring:Boot:Admin:Client:ConnectionTimeoutMs"] = "3500",
            ["Spring:Boot:Admin:Client:RefreshInterval"] = "00:01:00",
            ["Spring:Boot:Admin:Client:Metadata:user.name"] = "test-username",
            ["Spring:Boot:Admin:Client:Metadata:user.password"] = "test-password"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://spring-boot-admin-server.com:9090/instances").Respond("application/json", """{"Id":"1234567"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        await runner.RunAsync(TestContext.Current.CancellationToken);
        SpringBootAdminClientOptions? options = runner.LastGoodOptions;

        options.Should().NotBeNull();
        options.Url.Should().Be("http://spring-boot-admin-server.com:9090");
        options.ApplicationName.Should().Be("test-app");
        options.BaseUrl.Should().Be("http://www.test-app.com:8080/api");
        options.BaseScheme.Should().Be("http");
        options.BasePort.Should().Be(8080);
        options.BasePath.Should().Be("/api");
        options.BaseHost.Should().Be("www.test-app.com");
        options.UseNetworkInterfaces.Should().BeTrue();
        options.PreferIPAddress.Should().BeTrue();
        options.ValidateCertificates.Should().BeFalse();
        options.ConnectionTimeout.Should().Be(3500.Milliseconds());
        options.RefreshInterval.Should().Be(1.Minutes());
        options.Metadata.Should().HaveCount(2);
        options.Metadata.Should().ContainKey("user.name").WhoseValue.Should().Be("test-username");
        options.Metadata.Should().ContainKey("user.password").WhoseValue.Should().Be("test-password");

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task FailsOnMissingConfiguration()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        await using WebApplication app = builder.Build();
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        Func<Task> action = async () => await runner.RunAsync(TestContext.Current.CancellationToken);

        string[] errorsExpected =
        [
            "Url must be configured",
            "BaseUrl must be configured"
        ];

        await action.Should().ThrowExactlyAsync<OptionsValidationException>().WithMessage(string.Join("; ", errorsExpected));
    }

    [Fact]
    public async Task FailsOnInvalidConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "/spring-server-root",
            ["Spring:Boot:Admin:Client:BaseUrl"] = "/api",
            ["Spring:Boot:Admin:Client:BaseScheme"] = "ftp",
            ["Spring:Boot:Admin:Client:BasePort"] = "0"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSpringBootAdminClient();

        await using WebApplication app = builder.Build();
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        Func<Task> action = async () => await runner.RunAsync(TestContext.Current.CancellationToken);

        string[] errorsExpected =
        [
            "Url must be configured as an absolute URL",
            "BaseScheme must be null, 'http' or 'https'",
            "BasePort must be in range 1-65535",
            "BaseUrl must be configured as an absolute URL"
        ];

        await action.Should().ThrowExactlyAsync<OptionsValidationException>().WithMessage(string.Join("; ", errorsExpected));
    }

    [Fact]
    public async Task FailsWhenConfigurationForBasePathIsUrl()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://spring-boot-admin-server.com:9090",
            ["Spring:Boot:Admin:Client:BasePath"] = "http://api.localhost.com:1234/path"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<IServer, FakeServer>();
        builder.Services.AddSpringBootAdminClient();

        await using WebApplication app = builder.Build();
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        Func<Task> action = async () => await runner.RunAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<OptionsValidationException>()
            .WithMessage("Use BaseUrl instead of BasePath to configure the absolute URL to register with");
    }

    [Fact]
    public async Task BindsApplicationNameFromSpringConfiguration()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Application:Name"] = "spring-test-app",
            ["Spring:Boot:Admin:Client:Url"] = "http://spring-boot-admin-server.com:9090",
            ["Spring:Boot:Admin:Client:BaseUrl"] = "http://www.test-app.com:8080/api"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://spring-boot-admin-server.com:9090/instances").Respond("application/json", """{"Id":"1234567"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        await runner.RunAsync(TestContext.Current.CancellationToken);
        SpringBootAdminClientOptions? options = runner.LastGoodOptions;

        options.Should().NotBeNull();
        options.ApplicationName.Should().Be("spring-test-app");

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendsRegisterRequestForDefaultConfiguration()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(CurrentTime, CultureInfo.InvariantCulture));

        string expectedJson = JsonNode.Parse($$"""
            {
              "name": "{{CurrentAppName}}",
              "managementUrl": "http://localhost:5000/actuator",
              "healthUrl": "http://localhost:5000/actuator/health",
              "serviceUrl": "http://localhost:5000/",
              "metadata": {
                "startup": "{{CurrentTime}}"
              }
            }
            """)!.ToJsonString();

        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://spring-boot-admin-server.com:9090"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSingleton<IServer, FakeServer>();
        builder.Services.AddSingleton<IDomainNameResolver, FakeDomainNameResolver>();
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://spring-boot-admin-server.com:9090/instances")
            .WithHeaders("User-Agent", HttpClientExtensions.SteeltoeUserAgent).WithContent(expectedJson).Respond("application/json", """{"Id":"1234567"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        await runner.RunAsync(TestContext.Current.CancellationToken);

        runner.LastRegistrationId.Should().Be("1234567");
        runner.LastGoodOptions.Should().NotBeNull();

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task SendsRegisterRequestForCustomConfiguration()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse(CurrentTime, CultureInfo.InvariantCulture));

        string expectedJson = JsonNode.Parse($$"""
            {
              "name": "test-app",
              "managementUrl": "http://www.test-app.com:8080/alt-management",
              "healthUrl": "http://www.test-app.com:8080/alt-management/alt-health",
              "serviceUrl": "http://www.test-app.com:8080/api",
              "metadata": {
                "startup": "{{CurrentTime}}",
                "user.name": "test-username",
                "user.password": "test-password"
              }
            }
            """)!.ToJsonString();

        var appSettings = new Dictionary<string, string?>
        {
            ["Spring:Boot:Admin:Client:Url"] = "http://spring-boot-admin-server.com:9090",
            ["Spring:Boot:Admin:Client:ApplicationName"] = "test-app",
            ["Spring:Boot:Admin:Client:BaseUrl"] = "http://www.test-app.com:8080/api",
            ["Spring:Boot:Admin:Client:Metadata:user.name"] = "test-username",
            ["Spring:Boot:Admin:Client:Metadata:user.password"] = "test-password",
            ["Management:Endpoints:Path"] = "/alt-management",
            ["Management:Endpoints:Health:Path"] = "alt-health"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSingleton<TimeProvider>(timeProvider);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://spring-boot-admin-server.com:9090/instances")
            .WithHeaders("User-Agent", HttpClientExtensions.SteeltoeUserAgent).WithContent(expectedJson).Respond("application/json", """{"Id":"1234567"}""");

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);
        var runner = app.Services.GetRequiredService<SpringBootAdminRefreshRunner>();

        await runner.RunAsync(TestContext.Current.CancellationToken);

        runner.LastRegistrationId.Should().Be("1234567");
        runner.LastGoodOptions.Should().NotBeNull();

        handler.Mock.VerifyNoOutstandingExpectation();
    }
}
