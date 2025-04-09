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

        await File.WriteAllTextAsync(logFile.FullPath, "This is a test log.");
        await File.WriteAllTextAsync(tempLogFile.FullPath, "This is a test log.");

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(appSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/logfile"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string actualLogContent = await response.Content.ReadAsStringAsync();

        // assert
        actualLogContent.Trim().Should().Be("\"This is a test log.\"");
    }
}
