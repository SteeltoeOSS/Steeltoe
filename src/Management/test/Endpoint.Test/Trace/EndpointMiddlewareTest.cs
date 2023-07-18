// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public class EndpointMiddlewareTest : BaseTest
{
    private static readonly Dictionary<string, string> AppSettings = new()
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
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>()
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings)).ConfigureLogging(
                (webHostContext, loggingBuilder) =>
                {
                    loggingBuilder.AddConfiguration(webHostContext.Configuration);
                    loggingBuilder.AddDynamicConsole();
                });

        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();
        HttpResponseMessage result = await client.GetAsync(new Uri("http://localhost/actuator/httptrace"));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        string json = await result.Content.ReadAsStringAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<TraceEndpointOptions>();
        ManagementEndpointOptions managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().CurrentValue;
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/httptrace", options.GetPathMatchPattern(managementOptions.Path, managementOptions));
        Assert.Equal("/cloudfoundryapplication/httptrace", options.GetPathMatchPattern(ConfigureManagementEndpointOptions.DefaultCFPath, managementOptions));
        Assert.Contains("Get", options.AllowedVerbs);
    }

    [Fact]
    public void RoutesByPathAndVerbTrace()
    {
        TraceEndpointOptions options = GetOptionsMonitorFromSettings<TraceEndpointOptions>()
            .Get(ConfigureTraceEndpointOptions.TraceEndpointOptionNames.V1.ToString());

        ManagementEndpointOptions managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().CurrentValue;
        Assert.True(options.ExactMatch);
        Assert.Equal("/actuator/trace", options.GetPathMatchPattern(managementOptions.Path, managementOptions));
        Assert.Equal("/cloudfoundryapplication/trace", options.GetPathMatchPattern(ConfigureManagementEndpointOptions.DefaultCFPath, managementOptions));
        Assert.Contains("Get", options.AllowedVerbs);
    }
}
