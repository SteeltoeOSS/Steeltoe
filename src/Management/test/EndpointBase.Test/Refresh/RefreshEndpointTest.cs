// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Refresh.Test;

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
        IRefreshOptions options = null;
        const IConfigurationRoot configuration = null;

        Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration));

        options = new RefreshEndpointOptions();
        Assert.Throws<ArgumentNullException>(() => new RefreshEndpoint(options, configuration));
    }

    [Fact]
    public void DoInvoke_ReturnsExpected()
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
            services.AddRefreshActuatorServices(configuration);
        };
        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appsettings);
        };

        var ep = tc.GetService<IRefreshEndpoint>();
        var result = ep.Invoke();
        Assert.NotNull(result);

        Assert.Contains("management:endpoints:loggers:enabled", result);
        Assert.Contains("management:endpoints:cloudfoundry:enabled", result);
    }
}
