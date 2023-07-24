// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.Web.Hypermedia;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

public sealed class HypermediaEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public HypermediaEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        var ep = tc.GetRequiredService<IActuatorEndpointHandler>();

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
    public async Task Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
        };

        var ep = tc.GetRequiredService<IActuatorEndpointHandler>();

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
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:info:enabled", "false" }
            });
        };

        var ep = tc.GetRequiredService<IActuatorEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
        Assert.True(info.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", info.Entries["self"].Href);
        Assert.False(info.Entries.ContainsKey("info"));
        Assert.Single(info.Entries);
    }

    [Fact]
    public async Task Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        tc.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "management:endpoints:actuator:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var ep = tc.GetRequiredService<IActuatorEndpointHandler>();

        Links info = await ep.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
        Assert.Empty(info.Entries);
    }
}
