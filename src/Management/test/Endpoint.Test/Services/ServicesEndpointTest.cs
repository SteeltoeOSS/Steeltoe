// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Services;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class ServicesEndpointTest: BaseTest
{ 
    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        const IServiceCollection serviceCollection = null;

        IOptionsMonitor<ServicesEndpointOptions> options1 = null;

        Assert.Throws<ArgumentNullException>(() => new ServicesEndpointHandler(options1, serviceCollection, NullLogger<ServicesEndpointHandler>.Instance));
        IOptionsMonitor<ServicesEndpointOptions> options = GetOptionsMonitorFromSettings<ServicesEndpointOptions, ConfigureServicesEndpointOptions>();

        Assert.Throws<ArgumentNullException>(() => new ServicesEndpointHandler(options, serviceCollection, NullLogger<ServicesEndpointHandler>.Instance));
    }
}
