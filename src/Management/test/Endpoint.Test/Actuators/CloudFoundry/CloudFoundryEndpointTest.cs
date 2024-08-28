// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.CloudFoundry;

public sealed class CloudFoundryEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddInfoActuator();
            services.AddCloudFoundryActuator();
        };

        var handler = testContext.GetRequiredService<ICloudFoundryEndpointHandler>();

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
    public async Task Invoke_OnlyCloudFoundryEndpoint_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddCloudFoundryActuator();
        };

        var handler = testContext.GetRequiredService<ICloudFoundryEndpointHandler>();

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
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddCloudFoundryActuator();
            services.AddInfoActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:enabled", "true" }
            });
        };

        var handler = testContext.GetRequiredService<ICloudFoundryEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        Assert.NotNull(links);
        Assert.NotNull(links.Entries);
        Assert.True(links.Entries.ContainsKey("self"));
        Assert.Equal("http://localhost:5000/foobar", links.Entries["self"].Href);
        Assert.True(links.Entries.ContainsKey("info"));
        Assert.Equal(2, links.Entries.Count);
    }

    [Fact]
    public void Invoke_CloudFoundryDisable_DoesNotInvoke()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddCloudFoundryActuator();
            services.AddInfoActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:cloudfoundry:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var middleware = testContext.GetRequiredService<CloudFoundryEndpointMiddleware>();
        bool shouldInvoke = middleware.ShouldInvoke("/cloudfoundryapplication/info");
        Assert.False(shouldInvoke);
    }
}
