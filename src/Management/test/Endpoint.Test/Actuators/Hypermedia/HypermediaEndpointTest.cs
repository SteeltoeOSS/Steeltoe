// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class HypermediaEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuator();
            services.AddInfoActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:path", "foobar" }
            });
        };

        var handler = testContext.GetRequiredService<IHypermediaEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        links.Entries.Should().Contain(entry => entry.Key == "self" && entry.Value.Href == "http://localhost:5000/foobar");
        links.Entries.Should().Contain(entry => entry.Key == "info" && entry.Value.Href == "http://localhost:5000/foobar/info");
        links.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Invoke_OnlyActuatorHypermediaEndpoint_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuator();
        };

        var handler = testContext.GetRequiredService<IHypermediaEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        links.Entries.Should().Contain(entry => entry.Key == "self" && entry.Value.Href == "http://localhost:5000/foobar");
        links.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Invoke_HonorsEndpointEnabled_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuator();
            services.AddInfoActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:info:enabled", "false" }
            });
        };

        var handler = testContext.GetRequiredService<IHypermediaEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        links.Entries.Should().Contain(entry => entry.Key == "self" && entry.Value.Href == "http://localhost:5000/foobar");
        links.Entries.Should().ContainSingle();
    }

    [Fact]
    public async Task Invoke_CloudFoundryDisable_ReturnsExpectedLinks()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHypermediaActuator();
            services.AddInfoActuator();
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "management:endpoints:actuator:enabled", "false" },
                { "management:endpoints:info:enabled", "true" }
            });
        };

        var handler = testContext.GetRequiredService<IHypermediaEndpointHandler>();

        Links links = await handler.InvokeAsync("http://localhost:5000/foobar", CancellationToken.None);
        links.Entries.Should().BeEmpty();
    }
}
