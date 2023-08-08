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
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        var handler = testContext.GetRequiredService<IActuatorEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(links);
        Assert.NotNull(links.Entries);
        Assert.True(links.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", links.Entries["self"].Href);
        Assert.True(links.Entries.ContainsKey("info"));
        Assert.Equal("http://localhost:5000/foobar/info", links.Entries["info"].Href);
        Assert.Equal(2, links.Entries.Count);
    }

    [Fact]
    public async Task Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
        };

        var handler = testContext.GetRequiredService<IActuatorEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(links);
        Assert.NotNull(links.Entries);
        Assert.True(links.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", links.Entries["self"].Href);
        Assert.Single(links.Entries);
    }

    [Fact]
    public async Task Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:info:enabled", "false" }
            });
        };

        var handler = testContext.GetRequiredService<IActuatorEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(links);
        Assert.NotNull(links.Entries);
        Assert.True(links.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", links.Entries["self"].Href);
        Assert.False(links.Entries.ContainsKey("info"));
        Assert.Single(links.Entries);
    }

    [Fact]
    public async Task Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuatorServices();
            services.AddInfoActuatorServices();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:actuator:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var handler = testContext.GetRequiredService<IActuatorEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(links);
        Assert.NotNull(links.Entries);
        Assert.Empty(links.Entries);
    }
}
