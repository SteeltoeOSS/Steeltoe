// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ActuatorServiceCollectionExtensionsTest
{
    [Fact]
    public void AddAllActuators_ConfiguresCorsDefaults()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.ConfigureServices((context, services) => services.AddAllActuators(context.Configuration)).Build();
        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.True(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, m => m.Equals("GET"));
        Assert.Contains(policy.Methods, m => m.Equals("POST"));
    }

    [Fact]
    public void AddAllActuators_ConfiguresCorsCustom()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.ConfigureServices((context, services) =>
            services.AddAllActuators(context.Configuration, myPolicy => myPolicy.WithOrigins("http://google.com"))).Build();

        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.True(policy.IsOriginAllowed("http://google.com"));
        Assert.False(policy.IsOriginAllowed("http://bing.com"));
        Assert.False(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, m => m.Equals("GET"));
        Assert.Contains(policy.Methods, m => m.Equals("POST"));
    }

    [Fact]
    public void AddAllActuators_YesCF_onCF()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(cfg => cfg.AddCloudFoundry());

        IWebHost host = hostBuilder.ConfigureServices((context, services) => services.AddAllActuators(context.Configuration)).Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryOptions>());
        Assert.NotNull(host.Services.GetService<CloudFoundryEndpoint>());
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
    }

    [Fact]
    public void AddAllActuators_NoCF_offCF()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        }).ConfigureAppConfiguration(cfg => cfg.AddCloudFoundry());

        IWebHost host = hostBuilder.ConfigureServices((context, services) => services.AddAllActuators(context.Configuration)).Build();

        Assert.Null(host.Services.GetService<ICloudFoundryOptions>());
        Assert.Null(host.Services.GetService<CloudFoundryEndpoint>());
    }
}
