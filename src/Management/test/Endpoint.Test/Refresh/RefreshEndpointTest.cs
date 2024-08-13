// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Refresh;

public sealed class RefreshEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public RefreshEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

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

        using var testContext = new TestContext(_output);

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
