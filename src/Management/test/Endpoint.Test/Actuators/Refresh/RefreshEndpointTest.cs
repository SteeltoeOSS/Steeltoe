// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Refresh;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

public sealed class RefreshEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddRefreshActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var handler = testContext.GetRequiredService<IRefreshEndpointHandler>();
        IList<string> result = await handler.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(result);

        Assert.Contains("management:endpoints:loggers:enabled", result);
        Assert.Contains("management:endpoints:cloudfoundry:enabled", result);
    }
}
