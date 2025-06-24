// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryConfigurationProviderTest
{
    [Fact]
    public void Load_VCAP_APPLICATION_ChangesDataDictionary()
    {
        const string environment = """
            {
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                "application_name": "my-app",
                "application_uris": [ "my-app.10.244.0.34.xip.io"],
                "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                },
                "name": "my-app",
                "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                "space_name": "my-space",
                "uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                ],
                "users": null,
                "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        IDictionary<string, string?> properties = provider.Properties;
        Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", properties["vcap:application:application_id"]);
        Assert.Equal("1024", properties["vcap:application:limits:disk"]);
        Assert.Equal("my-app.10.244.0.34.xip.io", properties["vcap:application:uris:0"]);
        Assert.Equal("my-app2.10.244.0.34.xip.io", properties["vcap:application:uris:1"]);
    }

    [Fact]
    public void Load_VCAP_SERVICES_ChangesDataDictionary()
    {
        const string environment = """
            {
                "elephantsql": [{
                    "name": "elephantsql-c6c60",
                    "label": "elephantsql",
                    "tags": [
                        "postgres",
                        "postgresql",
                        "relational"
                    ],
                    "plan": "turtle",
                    "credentials": {"uri": "postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd"}
                }],
                "sendgrid": [{
                    "name": "mysendgrid",
                    "label": "sendgrid",
                    "tags": ["smtp"],
                    "plan": "free",
                    "credentials": {
                        "hostname": "smtp.sendgrid.net",
                        "username": "QvsXMbJ3rK",
                        "password": "HCHMOYluTv"
                    }
                }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        IDictionary<string, string?> properties = provider.Properties;
        Assert.Equal("elephantsql-c6c60", properties["vcap:services:elephantsql:0:name"]);
        Assert.Equal("mysendgrid", properties["vcap:services:sendgrid:0:name"]);
    }

    [Fact]
    public void Load_VCAP_SERVICES_MultiServices_ChangesDataDictionary()
    {
        const string environment = """
            {
                "p-config-server": [{
                    "name": "myConfigServer",
                    "label": "p-config-server",
                    "tags": ["configuration","spring-cloud"],
                    "plan": "standard",
                    "credentials": {
                        "uri": "https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.test-cloud.com",
                        "client_id": "p-config-server-9bff4c87-7ffd-4536-9e76-e67ea3ec81d0",
                        "client_secret": "AJUAjyxP3nO9",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    }
                }],
                "p-service-registry": [{
                    "name": "myServiceRegistry",
                    "label": "p-service-registry",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ],
                    "plan": "standard",
                    "credentials": {
                        "uri": "https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.test-cloud.com",
                        "client_id": "p-service-registry-9121b185-cd3b-497c-99f7-8e8064d4a6f0",
                        "client_secret": "3Rv1U79siLDa",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    }
                }],
                "p-mysql": [{
                    "name": "mySql1",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_0f5dda44_e678_4727_993f_30e6d455cc31",
                        "username": "9vD0Mtk3wFFuaaaY",
                        "password": "Cjn4HsAiKV8sImst",
                        "uri": "mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst"
                    }
                },
                {
                    "name": "mySql2",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_b2d83697_5fa1_4a51_991b_975c9d7e5515",
                        "username": "gxXQb2pMbzFsZQW8",
                        "password": "lvMkGf6oJQvKSOwn",
                        "uri": "mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?user=gxXQb2pMbzFsZQW8&password=lvMkGf6oJQvKSOwn"
                    }
                }]
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_SERVICES", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        IDictionary<string, string?> properties = provider.Properties;
        Assert.Equal("myConfigServer", properties["vcap:services:p-config-server:0:name"]);
        Assert.Equal("https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.test-cloud.com", properties["vcap:services:p-config-server:0:credentials:uri"]);
        Assert.Equal("myServiceRegistry", properties["vcap:services:p-service-registry:0:name"]);

        Assert.Equal("https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.test-cloud.com",
            properties["vcap:services:p-service-registry:0:credentials:uri"]);

        Assert.Equal("mySql1", properties["vcap:services:p-mysql:0:name"]);

        Assert.Equal("mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
            properties["vcap:services:p-mysql:0:credentials:uri"]);

        Assert.Equal("mySql2", properties["vcap:services:p-mysql:1:name"]);

        Assert.Equal("mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true",
            properties["vcap:services:p-mysql:1:credentials:uri"]);
    }

    [Fact]
    public void Load_VCAP_APPLICATION_Allows_Reload_Without_Throwing_Exception()
    {
        const string environment = """
            {
                "name": "my-app",
                "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
            }
            """;

        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", environment);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry();

        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        VcapApp? options = null;

        using var tokenSource = new CancellationTokenSource(250.Milliseconds());

        _ = Task.Run(() =>
        {
            // ReSharper disable once AccessToDisposedClosure
            while (!tokenSource.IsCancellationRequested)
            {
                configurationRoot.Reload();
            }
        }, tokenSource.Token);

        while (!tokenSource.IsCancellationRequested)
        {
            options = configurationRoot.GetSection("vcap:application").Get<VcapApp>();
        }

        Assert.NotNull(options);
        Assert.Equal("my-app", options.Name);
        Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
    }

    [Fact]
    public async Task ForwardedHeaders_enabled_on_CloudFoundry()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.AddCloudFoundryConfiguration();
        await using WebApplication host = builder.Build();

        ForwardedHeadersOptions options = host.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedFor);
        options.ForwardedHeaders.Should().HaveFlag(ForwardedHeaders.XForwardedProto);
        options.KnownNetworks.Should().BeEmpty();
        options.KnownProxies.Should().BeEmpty();
    }

    [Fact]
    public async Task ForwardedHeaders_not_enabled_off_CloudFoundry()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.AddCloudFoundryConfiguration();
        await using WebApplication host = builder.Build();

        ForwardedHeadersOptions options = host.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.Should().Be(ForwardedHeaders.None);
        options.KnownNetworks.Should().ContainSingle().Which.Should().BeEquivalentTo(IPNetwork.Parse("127.0.0.1/8"));
        options.KnownProxies.Should().ContainSingle().Which.Should().Be(IPAddress.Parse("::1"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ForwardedHeadersMiddleware_enabled_as_expected(bool onCloudFoundry)
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", onCloudFoundry ? "{}" : null);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.AddCloudFoundryConfiguration();
        await using WebApplication host = builder.Build();
        bool? isHttpRequest = null;

        host.Map("/", context =>
        {
            isHttpRequest = context.Request.IsHttps;
            return Task.CompletedTask;
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        TestServer server = host.GetTestServer();

        HttpContext context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = "https";
            c.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
            c.Connection.RemoteIpAddress = IPAddress.Loopback;
        }, TestContext.Current.CancellationToken);

        context.Response.StatusCode.Should().Be(200);
        isHttpRequest.Should().Be(onCloudFoundry, $"X-Forwarded-Proto should {(onCloudFoundry ? string.Empty : "not ")}be evaluated");
    }

    [Fact]
    public async Task ForwardedHeaders_configuration_can_be_overridden()
    {
        using var vcapScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.CreateDefault();
        builder.AddCloudFoundryConfiguration();

        builder.Services.Configure<ForwardedHeadersOptions>(options => options.KnownProxies.Add(IPAddress.Parse("192.168.1.2")));

        await using WebApplication host = builder.Build();
        bool? isHttpRequest = null;

        host.Map("/", context =>
        {
            isHttpRequest = context.Request.IsHttps;
            return Task.CompletedTask;
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        TestServer server = host.GetTestServer();

        HttpContext context = await server.SendAsync(c =>
        {
            c.Request.Headers["X-Forwarded-Proto"] = "https";
            c.Request.Headers["X-Forwarded-For"] = "1.2.3.4";
            c.Connection.RemoteIpAddress = IPAddress.Loopback;
        }, TestContext.Current.CancellationToken);

        context.Response.StatusCode.Should().Be(200);
        isHttpRequest.Should().Be(false, "X-Forwarded-Proto should not be evaluated");
    }

    private sealed class VcapApp
    {
#pragma warning disable S3459 // Unassigned members should be removed
#pragma warning disable S1144 // Unused private types or members should be removed
        public string? Name { get; set; }
        public string? Version { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
