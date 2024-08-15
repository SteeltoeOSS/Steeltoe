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

namespace Steeltoe.Management.Endpoint.Test;

public sealed class ActuatorServiceCollectionExtensionsTest
{
    [Fact]
    public void AddAllActuators_ConfiguresCorsDefaults()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        using IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();
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
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        using IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators(myPolicy => myPolicy.WithOrigins("http://google.com"))).Build();

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
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
                "limits": {
                    "fds": 16384,
                    "mem": 1024,
                    "disk": 1024
                },
                "application_name": "spring-cloud-broker",
                "application_uris": [
                    "spring-cloud-broker.apps.testcloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.testcloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """);

        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create().ConfigureAppConfiguration(builder => builder.AddCloudFoundry());

        using IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }

    [Fact]
    public void AddAllActuators_NoCF_offCF()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create().ConfigureAppConfiguration(builder => builder.AddCloudFoundry());

        using IWebHost host = hostBuilder.ConfigureServices(services => services.AddAllActuators()).Build();

        Assert.Null(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }
}
