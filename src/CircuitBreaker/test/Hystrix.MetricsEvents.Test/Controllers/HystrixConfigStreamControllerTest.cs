// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Configuration;
using Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Controllers;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.Test.Controllers;

public class HystrixConfigStreamControllerTest : HystrixTestBase
{
    [Fact]
    public void Constructor_SetsUpStream()
    {
        var stream = HystrixConfigurationStream.GetInstance();
        var controller = new HystrixConfigStreamController(stream);
        Assert.NotNull(controller.SampleStream);
    }

    [Fact]
    public async Task Endpoint_ReturnsHeaders()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>();
        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        client.BaseAddress = new Uri("http://localhost/");

        HttpResponseMessage result = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "hystrix/config.stream"), HttpCompletionOption.ResponseHeadersRead);

        Assert.NotNull(result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.True(result.Headers.Contains("Connection"));
        Assert.Contains("keep-alive", result.Headers.Connection);
        Assert.Equal("text/event-stream", result.Content.Headers.ContentType.MediaType);
        Assert.Equal("UTF-8", result.Content.Headers.ContentType.CharSet);
        Assert.True(result.Headers.CacheControl.NoCache);
        Assert.True(result.Headers.CacheControl.NoStore);
        Assert.Equal(new TimeSpan(0, 0, 0), result.Headers.CacheControl.MaxAge);
        Assert.True(result.Headers.CacheControl.MustRevalidate);
        result.Dispose();
    }

    [Fact]
    public void Endpoint_ReturnsData()
    {
        IWebHostBuilder builder = new WebHostBuilder().UseStartup<Startup>();
        using var server = new TestServer(builder);
        HttpClient client = server.CreateClient();

        client.BaseAddress = new Uri("http://localhost/");
        Stream result = client.GetStreamAsync("hystrix/config.stream").GetAwaiter().GetResult();

        HttpClient client2 = server.CreateClient();
        HttpResponseMessage cmdResult = client2.GetAsync("test/test.command").GetAwaiter().GetResult();
        Assert.Equal(HttpStatusCode.OK, cmdResult.StatusCode);

        var reader = new StreamReader(result);
        string data = reader.ReadLine();
        reader.Dispose();

        Assert.False(string.IsNullOrEmpty(data));
        Assert.StartsWith("data: ", data);
        string jsonObject = data.Substring(6);
        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonObject);
        Assert.NotNull(dict);

        Assert.NotNull(dict["type"]);
        Assert.Equal("HystrixConfig", dict["type"]);
        Assert.NotNull(dict["commands"]);
        Assert.NotNull(dict["threadpools"]);
        var commands = (JObject)dict["commands"];
        Assert.NotNull(commands["MyCommand"]);
    }
}
