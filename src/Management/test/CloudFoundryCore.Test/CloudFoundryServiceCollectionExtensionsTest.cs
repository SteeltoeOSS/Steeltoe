// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Management.CloudFoundry.Test;

[Obsolete("To be removed in the next major version.")]
public class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public void AddCloudFoundryActuators_ThrowsOnNull_Services()
    {
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => CloudFoundryServiceCollectionExtensions.AddCloudFoundryActuators(null, config));
        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddCloudFoundryActuators_ThrowsOnNull_Config()
    {
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services2.AddCloudFoundryActuators());

        Assert.Equal("config", ex.ParamName);
    }

    [Fact]
    public void AddCloudFoundryActuators_ConfiguresCorsDefaults()
    {
        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.ConfigureServices((context, services) => services.AddCloudFoundryActuators(context.Configuration)).Build();
        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.True(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, m => m.Equals("GET"));
        Assert.Contains(policy.Methods, m => m.Equals("POST"));
    }

    [Fact]
    public void AddCloudFoundryActuators_ConfiguresCorsCustom()
    {
        Action<CorsPolicyBuilder> customCors = myPolicy => myPolicy.WithOrigins("http://google.com");

        IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
        {
        });

        IWebHost host = hostBuilder.ConfigureServices((context, services) => services.AddCloudFoundryActuators(context.Configuration, customCors)).Build();
        var options = new ApplicationBuilder(host.Services).ApplicationServices.GetService(typeof(IOptions<CorsOptions>)) as IOptions<CorsOptions>;

        Assert.NotNull(options);
        CorsPolicy policy = options.Value.GetPolicy("SteeltoeManagement");
        Assert.True(policy.IsOriginAllowed("http://google.com"));
        Assert.False(policy.IsOriginAllowed("http://bing.com"));
        Assert.False(policy.IsOriginAllowed("*"));
        Assert.Contains(policy.Methods, m => m.Equals("GET"));
        Assert.Contains(policy.Methods, m => m.Equals("POST"));
    }
}
