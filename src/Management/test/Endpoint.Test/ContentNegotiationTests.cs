// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Steeltoe.Management.Endpoint.Actuators.Loggers;
using Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ContentNegotiationTests
{
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Management:Endpoints:Actuator:Exposure:Include:0"] = "*"
    };

    [Fact]
    public async Task Can_use_content_type_with_alternate_casing()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("APPLICATION/vnd.Spring-Boot.Actuator.v3+JSON");
        HttpContent requestContent = new StringContent("{}", contentType);
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Can_use_content_type_including_charset()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("application/vnd.spring-boot.actuator.v3+json; charset=utf-8");
        HttpContent requestContent = new StringContent("{}", contentType);
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Cannot_use_invalid_content_type()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        MediaTypeHeaderValue contentType = MediaTypeHeaderValue.Parse("application/xhtml+xml");
        HttpContent requestContent = new StringContent("{}", contentType);
        HttpResponseMessage response = await client.PostAsync(new Uri("http://localhost/actuator/loggers/Default"), requestContent);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Be("Only the 'application/vnd.spring-boot.actuator.v3+json' content type is supported.");
    }

    [Fact]
    public async Task Can_omit_accept_header()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/loggers"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cannot_use_incompatible_accept_header()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("http://localhost/actuator/loggers"),
            Headers =
            {
                Accept =
                {
                    MediaTypeWithQualityHeaderValue.Parse("text/html"),
                    MediaTypeWithQualityHeaderValue.Parse("application/xhtml+xml")
                }
            }
        };

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
        string responseText = await response.Content.ReadAsStringAsync();
        responseText.Should().Be("Only the 'application/vnd.spring-boot.actuator.v3+json' content type is supported.");
    }

    [Fact]
    public async Task Can_use_compatible_accept_header()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("http://localhost/actuator/loggers"),
            Headers =
            {
                Accept =
                {
                    MediaTypeWithQualityHeaderValue.Parse("text/html; q=0.8"),
                    MediaTypeWithQualityHeaderValue.Parse("application/json; q=0.1")
                }
            }
        };

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Can_use_wildcard_accept_header()
    {
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(AppSettings);
        builder.Services.AddLoggersActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("http://localhost/actuator/loggers"),
            Headers =
            {
                Accept =
                {
                    MediaTypeWithQualityHeaderValue.Parse("text/html"),
                    MediaTypeWithQualityHeaderValue.Parse("*/*; q=0.8")
                }
            }
        };

        using HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.SendAsync(requestMessage);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cannot_use_incompatible_accept_header_in_heap_dump_actuator()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["management:endpoints:heapDump:heapDumpType"] = "gcdump"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHeapDumpActuator();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/heapdump"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
    }

    [Fact]
    public async Task Can_use_compatible_accept_header_in_heap_dump_actuator()
    {
        var appSettings = new Dictionary<string, string?>(AppSettings)
        {
            ["management:endpoints:heapDump:heapDumpType"] = "gcdump"
        };

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddHeapDumpActuator();
        builder.Services.AddSingleton<IHeapDumper, FakeHeapDumper>();

        await using WebApplication host = builder.Build();
        await host.StartAsync();

        using HttpClient client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://localhost/actuator/heapdump"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
