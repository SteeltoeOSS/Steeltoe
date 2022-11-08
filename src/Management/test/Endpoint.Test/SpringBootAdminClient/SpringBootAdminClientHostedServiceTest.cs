// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Configuration;
using RichardSzalay.MockHttp;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public class SpringBootAdminClientHostedServiceTest
{
    [Fact]
    public async Task SpringBootAdminClient_RegistersAndDeletes()
    {
        try
        {
            var appSettings = new Dictionary<string, string>
            {
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:health:path"] = "myhealth",
                ["URLS"] = "http://localhost:8080;https://localhost:8082",
                ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
                ["spring:application:name"] = "MySteeltoeApplication"
            };

            IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
            var appInfo = new ApplicationInstanceInfo(configurationRoot);
            var sbaOptions = new SpringBootAdminClientOptions(configurationRoot, appInfo);
            var managementOptions = new ManagementEndpointOptions(configurationRoot);
            var healthOptions = new HealthEndpointOptions(configurationRoot);
            var httpMessageHandler = new MockHttpMessageHandler();
            httpMessageHandler.Expect(HttpMethod.Post, "http://springbootadmin:9090/instances").Respond("application/json", "{\"Id\":\"1234567\"}");

            httpMessageHandler.Expect(HttpMethod.Delete, "http://springbootadmin:9090/instances/1234567")
                .Respond(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

            Assert.Null(SpringBootAdminClientHostedService.RegistrationResult);
            var service = new SpringBootAdminClientHostedService(sbaOptions, managementOptions, healthOptions, httpMessageHandler.ToHttpClient());
            await service.StartAsync(default);
            await service.StopAsync(default);

            httpMessageHandler.VerifyNoOutstandingExpectation();
            Assert.Equal("1234567", SpringBootAdminClientHostedService.RegistrationResult?.Id);
        }
        finally
        {
            SpringBootAdminClientHostedService.RegistrationResult = null;
        }
    }

    [Fact]
    public async Task SpringBootAdminClient_DoesNotThrow_WhenNoServerRunning()
    {
        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "myhealth",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:application:name"] = "MySteeltoeApplication"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var appInfo = new ApplicationInstanceInfo(configurationRoot);
        var sbaOptions = new SpringBootAdminClientOptions(configurationRoot, appInfo);
        var managementOptions = new ManagementEndpointOptions(configurationRoot);
        var healthOptions = new HealthEndpointOptions(configurationRoot);
        var httpMessageHandler = new MockHttpMessageHandler();

        httpMessageHandler.Expect(HttpMethod.Post, "http://springbootadmin:9090/instances")
            .Throw(new HttpRequestException("No connection could be made because the target machine actively refused it."));

        Assert.Null(SpringBootAdminClientHostedService.RegistrationResult);
        var service = new SpringBootAdminClientHostedService(sbaOptions, managementOptions, healthOptions, httpMessageHandler.ToHttpClient());
        await service.StartAsync(default);

        httpMessageHandler.VerifyNoOutstandingExpectation();
    }
}
