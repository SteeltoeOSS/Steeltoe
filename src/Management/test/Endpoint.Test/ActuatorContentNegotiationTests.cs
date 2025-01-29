// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.All;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorContentNegotiationTests
{
    private const string ActuatorV1 = "application/vnd.spring-boot.actuator.v1+json";
    private const string ActuatorV2 = "application/vnd.spring-boot.actuator.v2+json";
    private const string ActuatorV3 = "application/vnd.spring-boot.actuator.v3+json";
    private const string SpringBootStandardAccept = $"{ActuatorV2},{ActuatorV1},application/json";

    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*"
    };

    // AcceptHeader values captured from Spring Boot Admin 3.4.1
    [InlineData("", $"{ActuatorV3},{SpringBootStandardAccept}", ActuatorV3)]
    [InlineData("beans", SpringBootStandardAccept, ActuatorV3)]
    [InlineData("env", SpringBootStandardAccept, ActuatorV3)]
    [InlineData("health", $"{ActuatorV3},{SpringBootStandardAccept}", ActuatorV3)]
    [InlineData("heapdump",
        "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
        "application/octet-stream")]
    [InlineData("httpexchanges", SpringBootStandardAccept, ActuatorV3)]
    [InlineData("info", $"{ActuatorV3},{SpringBootStandardAccept}", ActuatorV3)]
    [InlineData("loggers", SpringBootStandardAccept, ActuatorV3)]
    [InlineData("mappings", SpringBootStandardAccept, ActuatorV2)]
    [InlineData("metrics", SpringBootStandardAccept, ActuatorV3)]
    [InlineData("threaddump", SpringBootStandardAccept, ActuatorV3)]
    [Theory]
    public async Task Responses_for_SpringBootAdmin_match_expectations(string endpoint, string acceptHeader, string responseContentType)
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddAllActuators();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"http://localhost/actuator/{endpoint}")
        };

        foreach (string acceptValue in acceptHeader.Split(','))
        {
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptValue));
        }

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be(responseContentType);
    }

    // Values captured from Spring Boot Admin 3.4.1
    [Fact]
    public async Task SpringBootAdmin_Loggers_Post_matches_expectations()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddAllActuators();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("http://localhost/actuator/loggers/Microsoft"),
            Content = new StringContent("""{"configuredLevel":"ERROR"}""", MediaTypeHeaderValue.Parse("application/json"))
        };

        foreach (string acceptValue in SpringBootStandardAccept.Split(','))
        {
            requestMessage.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(acceptValue));
        }

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Content.Headers.ContentType.Should().BeNull();
    }
}
