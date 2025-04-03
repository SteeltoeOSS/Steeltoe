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
    private static readonly Dictionary<string, string?> AppSettings = new()
    {
        ["Logging:Console:IncludeScopes"] = "false",
        ["Logging:LogLevel:Default"] = "Warning",
        ["Logging:LogLevel:TestApp"] = "Information",
        ["Logging:LogLevel:Steeltoe"] = "Information",
        ["management:endpoints:enabled"] = "true",
        ["management:endpoints:actuator:exposure:include:0"] = "*"
    };

    private readonly TempFile _tempLogFile = new();

    [Fact]
    public async Task LogfileActuator_ReturnsExpectedData()
    {
        AppSettings["management:endpoints:logfile:filePath"] = _tempLogFile.FullPath;
        await File.WriteAllTextAsync(_tempLogFile.FullPath, "This is a test log.");

        WebHostBuilder builder = TestWebHostBuilderFactory.Create();
        builder.UseStartup<Startup>();
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(AppSettings));

        using IWebHost app = builder.Build();
        await app.StartAsync();

        using HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync(new Uri("http://localhost/actuator/logfile"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string actualLogContent = await response.Content.ReadAsStringAsync();

        // assert
        actualLogContent.Trim().Should().BeEquivalentTo("\"This is a test log.\"");
    }
}
