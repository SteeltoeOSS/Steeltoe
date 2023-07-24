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

public sealed class CloudFoundryEndpointTest : BaseTest
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

        tc.AdditionalServices = (services, _) =>
        {
            services.AddInfoActuatorServices();
            services.AddCloudFoundryActuatorServices();
        };

        var ep = tc.GetRequiredService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
        Assert.True(info.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.Entries["self"].Href);
        Assert.True(info.Entries.ContainsKey("info"));
        Assert.Equal("http://localhost:5000/foobar/info", info.Entries["info"].Href);
        Assert.Equal(2, info.Entries.Count);
    }

    [Fact]
    public async Task Invoke_OnlyCloudFoundryEndpoint_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddCloudFoundryActuatorServices();
        };

        var ep = tc.GetRequiredService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
        Assert.True(info.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.Entries["self"].Href);
        Assert.Single(info.Entries);
    }

    [Fact]
    public async Task Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
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

        var ep = tc.GetRequiredService<ICloudFoundryEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
        Assert.True(info.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.Entries["self"].Href);
        Assert.True(info.Entries.ContainsKey("info"));
        Assert.Equal(2, info.Entries.Count);
    }

    [Fact]
    public void Invoke_CloudFoundryDisable_DoesNotInvoke()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
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

        var middle = tc.GetRequiredService<CloudFoundryEndpointMiddleware>();
        bool shouldInvoke = middle.ShouldInvoke(new PathString("/cloudfoundryapplication/info"));
        Assert.False(shouldInvoke);
    }
}
