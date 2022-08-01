// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test;

public class CloudFoundryEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public CloudFoundryEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsOptionsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpoint(null, null));
        Assert.Throws<ArgumentNullException>(() => new CloudFoundryEndpoint(new CloudFoundryEndpointOptions(), null));
    }

    [Fact]
    public void Invoke_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddInfoActuatorServices(configuration);
            services.AddCloudFoundryActuatorServices(configuration);
        };

        var cloudFoundryOptions = tc.GetService<CloudFoundryManagementOptions>();
        cloudFoundryOptions.EndpointOptions.Add(tc.GetService<IInfoOptions>());

        var ep = tc.GetService<ICloudFoundryEndpoint>();

        var info = ep.Invoke("http://localhost:5000/foobar");
        Assert.NotNull(info);
        Assert.NotNull(info._links);
        Assert.True(info._links.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
        Assert.True(info._links.ContainsKey("info"));
        Assert.Equal("http://localhost:5000/foobar/info", info._links["info"].Href);
        Assert.Equal(2, info._links.Count);
    }

    [Fact]
    public void Invoke_OnlyCloudFoundryEndpoint_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new CloudFoundryManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<ICloudFoundryOptions>());

                return options;
            });
        };
        var ep = tc.GetService<ICloudFoundryEndpoint>();

        var info = ep.Invoke("http://localhost:5000/foobar");
        Assert.NotNull(info);
        Assert.NotNull(info._links);
        Assert.True(info._links.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
        Assert.Single(info._links);
    }

    [Fact]
    public void Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices(configuration);
            services.AddInfoActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new CloudFoundryManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IInfoOptions>());
                options.EndpointOptions.Add(sp.GetRequiredService<ICloudFoundryOptions>());

                return options;
            });
        };
        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:enabled", "true" }
            });
        };
        var ep = tc.GetService<ICloudFoundryEndpoint>();

        var info = ep.Invoke("http://localhost:5000/foobar");
        Assert.NotNull(info);
        Assert.NotNull(info._links);
        Assert.True(info._links.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info._links["self"].Href);
        Assert.False(info._links.ContainsKey("info"));
        Assert.Single(info._links);
    }

    [Fact]
    public void Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices(configuration);
            services.AddInfoActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new CloudFoundryManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IInfoOptions>());
                options.EndpointOptions.Add(sp.GetRequiredService<ICloudFoundryOptions>());

                return options;
            });
        };
        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var ep = tc.GetService<ICloudFoundryEndpoint>();

        var info = ep.Invoke("http://localhost:5000/foobar");
        Assert.NotNull(info);
        Assert.NotNull(info._links);
        Assert.Empty(info._links);
    }
}
