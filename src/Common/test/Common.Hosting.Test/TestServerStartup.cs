// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Common.Hosting.Test;

public class TestServerStartup
{
    public List<string> ExpectedAddresses { get; private set; }

    public TestServerStartup()
    {
        ExpectedAddresses = new List<string>();
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app)
    {
        List<string> addresses = ExpectedAddresses;
        Assert.Equal(addresses, app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses);
        ExpectedAddresses = null;
    }
}
