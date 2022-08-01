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

namespace Steeltoe.Management.Endpoint.Hypermedia.Test;

public class HypermediaEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public HypermediaEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsOptionsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ActuatorEndpoint(null, null));
    }

    [Fact]
    public void Invoke_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddHypermediaActuatorServices(configuration);
            services.AddInfoActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new ActuatorManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IInfoOptions>());
                options.EndpointOptions.Add(sp.GetRequiredService<IActuatorHypermediaOptions>());

                return options;
            });
        };

        var ep = tc.GetService<IActuatorEndpoint>();

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
    public void Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddHypermediaActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new ActuatorManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IActuatorHypermediaOptions>());

                return options;
            });
        };

        var ep = tc.GetService<IActuatorEndpoint>();

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
            services.AddHypermediaActuatorServices(configuration);
            services.AddInfoActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new ActuatorManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IInfoOptions>());
                options.EndpointOptions.Add(sp.GetRequiredService<IActuatorHypermediaOptions>());

                return options;
            });
        };
        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:info:enabled", "false" }
            });
        };

        var ep = tc.GetService<IActuatorEndpoint>();

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
            services.AddHypermediaActuatorServices(configuration);
            services.AddInfoActuatorServices(configuration);
            services.AddSingleton(sp =>
            {
                var options = new ActuatorManagementOptions();
                options.EndpointOptions.Add(sp.GetRequiredService<IInfoOptions>());
                options.EndpointOptions.Add(sp.GetRequiredService<IActuatorHypermediaOptions>());

                return options;
            });
        };
        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:actuator:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var ep = tc.GetService<IActuatorEndpoint>();

        var info = ep.Invoke("http://localhost:5000/foobar");
        Assert.NotNull(info);
        Assert.NotNull(info._links);
        Assert.Empty(info._links);
    }
}
