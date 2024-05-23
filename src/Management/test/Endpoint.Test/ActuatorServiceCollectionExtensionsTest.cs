// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorServiceCollectionExtensionsTest
{
    [Fact]
    public void AddAllActuators_ConfiguresCorsDefaults()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(HostingHelpers.EmptyAction);

        IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();
        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy? policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.NotNull(policy);
        Assert.True(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, method => method == "GET");
        Assert.Contains(policy.Methods, method => method == "POST");
    }

    [Fact]
    public void AddAllActuators_ConfiguresCorsCustom()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(HostingHelpers.EmptyAction);

        IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators(myPolicy => myPolicy.WithOrigins("http://google.com"))).Build();

        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy? policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.NotNull(policy);
        Assert.True(policy.IsOriginAllowed("http://google.com"));
        Assert.False(policy.IsOriginAllowed("http://bing.com"));
        Assert.False(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, method => method == "GET");
        Assert.Contains(policy.Methods, method => method == "POST");
    }

    [Fact]
    public void AddAllActuators_YesCF_onCF()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);

        IWebHostBuilder hostBuilder =
            new WebHostBuilder().Configure(HostingHelpers.EmptyAction).ConfigureAppConfiguration(builder => builder.AddCloudFoundry());

        IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }

    [Fact]
    public void AddAllActuators_NoCF_offCF()
    {
        IWebHostBuilder hostBuilder =
            new WebHostBuilder().Configure(HostingHelpers.EmptyAction).ConfigureAppConfiguration(builder => builder.AddCloudFoundry());

        IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();

        Assert.Null(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }
}
