// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class CloudFoundryEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public CloudFoundryEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddInfoActuatorServices();
            services.AddCloudFoundryActuatorServices();
        };

        var ep = tc.GetService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.LinkCollection);
        Assert.True(info.LinkCollection.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.LinkCollection["self"].Href);
        Assert.True(info.LinkCollection.ContainsKey("info"));
        Assert.Equal("http://localhost:5000/foobar/info", info.LinkCollection["info"].Href);
        Assert.Equal(2, info.LinkCollection.Count);
    }

    [Fact]
    public async Task Invoke_OnlyCloudFoundryEndpoint_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices();
        };

        var ep = tc.GetService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.LinkCollection);
        Assert.True(info.LinkCollection.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.LinkCollection["self"].Href);
        Assert.Single(info.LinkCollection);
    }

    [Fact]
    public async Task Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices();
            services.AddInfoActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:enabled", "true" }
            });
        };

        var ep = tc.GetService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.LinkCollection);
        Assert.True(info.LinkCollection.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.LinkCollection["self"].Href);
        Assert.True(info.LinkCollection.ContainsKey("info"));
        Assert.Equal(2, info.LinkCollection.Count);
    }

    [Fact]
    public void Invoke_CloudFoundryDisable_DoesNotInvoke()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddCloudFoundryActuatorServices();
            services.AddInfoActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:cloudfoundry:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var middle = tc.GetService<CloudFoundryEndpointMiddleware>();
        bool shouldInvoke = middle.ShouldInvoke(new PathString("/cloudfoundryapplication/info"));
        Assert.False(shouldInvoke);
    }
}
