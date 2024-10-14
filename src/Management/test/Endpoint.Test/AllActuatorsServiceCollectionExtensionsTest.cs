// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.All;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

namespace Steeltoe.Management.Endpoint.Test;

public sealed class AllActuatorsServiceCollectionExtensionsTest
{
    [Fact]
    public void AddAllActuators_ConfiguresCorsDefaults()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureServices(services => services.AddAllActuators());
        using IWebHost host = hostBuilder.Build();

        var options = host.Services.GetService<IOptions<CorsOptions>>();

        Assert.NotNull(options);
        CorsPolicy? policy = options.Value.GetPolicy(CorsServiceCollectionExtensions.ActuatorsCorsPolicyName);
        Assert.NotNull(policy);
        Assert.True(policy.IsOriginAllowed("*"));
        Assert.Equal(2, policy.Methods.Count);
        Assert.Contains(policy.Methods, method => method == "GET");
        Assert.Contains(policy.Methods, method => method == "POST");
    }

    [Fact]
    public void AddAllActuators_ConfiguresCorsCustom()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddActuatorsCorsPolicy(myPolicy => myPolicy.WithOrigins("http://google.com"));
            services.AddAllActuators();
        });

        using IWebHost host = hostBuilder.Build();

        var options = host.Services.GetService<IOptions<CorsOptions>>();

        Assert.NotNull(options);
        CorsPolicy? policy = options.Value.GetPolicy(CorsServiceCollectionExtensions.ActuatorsCorsPolicyName);
        Assert.NotNull(policy);
        Assert.True(policy.IsOriginAllowed("http://google.com"));
        Assert.False(policy.IsOriginAllowed("http://bing.com"));
        Assert.False(policy.IsOriginAllowed("*"));
        Assert.Equal(2, policy.Methods.Count);
        Assert.Contains(policy.Methods, method => method == "GET");
        Assert.Contains(policy.Methods, method => method == "POST");
    }

    [Fact]
    public void AddAllActuators_ConfiguresCorsCustom_ThrowsWhenTooLate()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();

        hostBuilder.ConfigureServices(services =>
        {
            services.AddAllActuators();
            services.AddActuatorsCorsPolicy(myPolicy => myPolicy.WithOrigins("http://google.com"));
        });

        using IWebHost host = hostBuilder.Build();
        var options = host.Services.GetRequiredService<IOptions<CorsOptions>>();

        Action action = () => _ = options.Value;

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("A CORS policy for actuator endpoints has already been configured. Call 'AddActuatorsCorsPolicy()' before adding actuators.");
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
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "name": "spring-cloud-broker",
                "space_name": "p-spring-cloud-services",
                "space_id": "65b73473-94cc-4640-b462-7ad52838b4ae",
                "uris": [
                    "spring-cloud-broker.apps.test-cloud.com"
                ],
                "users": null,
                "version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_version": "07e112f7-2f71-4f5a-8a34-db51dbed30a3",
                "application_id": "798c2495-fe75-49b1-88da-b81197f2bf06"
            }
            """);

        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddCloudFoundry());
        hostBuilder.ConfigureServices(services => services.AddAllActuators());
        using IWebHost host = hostBuilder.Build();

        Assert.NotNull(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }

    [Fact]
    public void AddAllActuators_NoCF_offCF()
    {
        IWebHostBuilder hostBuilder = TestWebHostBuilderFactory.Create();
        hostBuilder.ConfigureAppConfiguration(builder => builder.AddCloudFoundry());
        hostBuilder.ConfigureServices(services => services.AddAllActuators());

        using IWebHost host = hostBuilder.Build();

        Assert.Null(host.Services.GetService<ICloudFoundryEndpointHandler>());
    }
}
