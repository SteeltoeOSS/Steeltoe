// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Refresh;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Refresh;

public class RefreshEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public RefreshEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        const IConfigurationRoot configuration = null;

        IOptionsMonitor<RefreshEndpointOptions> options1 = null;

        Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options1, configuration, NullLoggerFactory.Instance));
        IOptionsMonitor<RefreshEndpointOptions> options = GetOptionsMonitorFromSettings<RefreshEndpointOptions, ConfigureRefreshEndpointOptions>();

        Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration, NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task DoInvoke_ReturnsExpected()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:loggers:enabled"] = "false",
            ["management:endpoints:heapdump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validatecertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddRefreshActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetService<IRefreshEndpoint>();
        IList<string> result = await ep.InvokeAsync(CancellationToken.None);
        Assert.NotNull(result);

        Assert.Contains("management:endpoints:loggers:enabled", result);
        Assert.Contains("management:endpoints:cloudfoundry:enabled", result);
    }
}
