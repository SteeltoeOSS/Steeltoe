// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class SpringBootAdminClientHostedServiceTest : BaseTest
{
    [Fact]
    public async Task SpringBootAdminClient_RegistersAndDeletes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "my-health",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:application:name"] = "MySteeltoeApplication"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://springbootadmin:9090/instances").Respond("application/json", """{"Id":"1234567"}""");
        handler.Mock.Expect(HttpMethod.Delete, "http://springbootadmin:9090/instances/1234567").Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        SpringBootAdminClientHostedService hostedService = app.Services.GetServices<IHostedService>().OfType<SpringBootAdminClientHostedService>().Single();
        Assert.Null(hostedService.RegistrationResult);

        await hostedService.StartAsync(default);
        await hostedService.StopAsync(default);

        handler.Mock.VerifyNoOutstandingExpectation();
        Assert.Equal("1234567", hostedService.RegistrationResult?.Id);
    }

    [Fact]
    public async Task SpringBootAdminClient_DoesNotThrow_WhenNoServerRunning()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "my-health",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:application:name"] = "MySteeltoeApplication"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddSpringBootAdminClient();

        using var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "http://springbootadmin:9090/instances")
            .Throw(new HttpRequestException("No connection could be made because the target machine actively refused it."));

        await using WebApplication app = builder.Build();
        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        SpringBootAdminClientHostedService hostedService = app.Services.GetServices<IHostedService>().OfType<SpringBootAdminClientHostedService>().Single();
        Assert.Null(hostedService.RegistrationResult);

        await hostedService.StartAsync(default);

        handler.Mock.VerifyNoOutstandingExpectation();
    }
}
