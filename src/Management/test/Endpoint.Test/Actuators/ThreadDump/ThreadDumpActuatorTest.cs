// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class ThreadDumpActuatorTest
{
    [Fact]
    public async Task Configures_default_settings()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Services.AddThreadDumpActuator();
        await using WebApplication host = builder.Build();

        ThreadDumpEndpointOptions options = host.Services.GetRequiredService<IOptions<ThreadDumpEndpointOptions>>().Value;

        options.Duration.Should().Be(10);
        options.Enabled.Should().BeNull();
        options.Id.Should().Be("threaddump");
        options.Path.Should().Be("threaddump");
        options.RequiredPermissions.Should().Be(EndpointPermissions.Restricted);

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

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddThreadDumpActuator();
        await using WebApplication host = builder.Build();

        ThreadDumpEndpointOptions options = host.Services.GetRequiredService<IOptions<ThreadDumpEndpointOptions>>().Value;

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
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Actuator:Exposure:Include:0"] = "threaddump"
        };

        await using HostWrapper host = hostBuilderType.Build(builder =>
        {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(appSettings));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IThreadDumper, FakeThreadDumper>();
                services.AddThreadDumpActuator();
            });
        });

        await host.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient httpClient = host.GetTestClient();

        HttpResponseMessage response = await httpClient.GetAsync(new Uri("http://localhost/actuator/threaddump"), TestContext.Current.CancellationToken);

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
}
