// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using RichardSzalay.MockHttp;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Health;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test;

public class SpringBootAdminClientHostedServiceTest
{
    [Fact]
    public async Task SpringBootAdminClient_RegistersAndDeletes()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:path"] = "/management",
            ["management:endpoints:health:path"] = "myhealth",
            ["URLS"] = "http://localhost:8080;https://localhost:8082",
            ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
            ["spring:application:name"] = "MySteeltoeApplication",
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
        var appInfo = new ApplicationInstanceInfo(config);
        var sbaOptions = new SpringBootAdminClientOptions(config, appInfo);
        var mgmtOptions = new ManagementEndpointOptions(config);
        var healthOptions = new HealthEndpointOptions(config);
        var httpMessageHandler = new MockHttpMessageHandler();
        httpMessageHandler
            .Expect(HttpMethod.Post, "http://springbootadmin:9090/instances")
            .Respond("application/json", "{\"Id\":\"1234567\"}");
        httpMessageHandler
            .Expect(HttpMethod.Delete, "http://springbootadmin:9090/instances/1234567")
            .Respond(req => new HttpResponseMessage(HttpStatusCode.NoContent));

        Assert.Null(SpringBootAdminClientHostedService.RegistrationResult);
        var service = new SpringBootAdminClientHostedService(sbaOptions, mgmtOptions, healthOptions, httpMessageHandler.ToHttpClient());
        await service.StartAsync(default);
        await service.StopAsync(default);

        httpMessageHandler.VerifyNoOutstandingExpectation();
        Assert.Equal("1234567", SpringBootAdminClientHostedService.RegistrationResult.Id);
    }
}
