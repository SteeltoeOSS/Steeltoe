// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.TestResources;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Actuators.Trace;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

public sealed class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:Pivotal"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:trace:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "httptrace"
    };

    [Fact]
    public async Task TraceActuator_ReturnsExpectedData()
    {
        IWebHostBuilder builder = TestWebHostBuilderFactory.Create().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/httptrace"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<TraceEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/httptrace", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/httptrace",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerbTrace()
    {
        TraceEndpointOptions endpointOptions = GetOptionsMonitorFromSettings<TraceEndpointOptions>()
            .Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.True(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/trace", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/trace",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }
}
