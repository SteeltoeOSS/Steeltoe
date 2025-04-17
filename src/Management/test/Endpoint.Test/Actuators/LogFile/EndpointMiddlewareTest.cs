// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class EndpointMiddlewareTest : BaseTest
{

    [Fact]
    public async Task LogFileActuator_ReturnsExpectedData()
    {
        using var logFile = new TempFile();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*",
            ["management:endpoints:logfile:filePath"] = logFile.FullPath
        };

        await File.WriteAllTextAsync(logFile.FullPath, "This is a test log.", TestContext.Current.CancellationToken);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/logfile"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string actualLogContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        actualLogContent.Should().Be("This is a test log.");
    }

    [Fact]
    public async Task LogFileActuator_HeadRequest_ReturnsExpectedData()
    {
        using var logFile = new TempFile();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*",
            ["management:endpoints:logfile:filePath"] = logFile.FullPath
        };

        await File.WriteAllBytesAsync(logFile.FullPath, "This is a test log."u8.ToArray(), TestContext.Current.CancellationToken);

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage headRequest = new HttpRequestMessage(HttpMethod.Head, new Uri("http://localhost/actuator/logfile"));
        HttpResponseMessage response = await client.SendAsync(headRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.Equal("bytes", response.Headers.AcceptRanges.Single());
        Assert.Equal(19, response.Content.Headers.ContentLength);
    }
}
