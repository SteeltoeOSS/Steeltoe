// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

public sealed class InfoEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public InfoEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_NoContributors_ReturnsExpectedInfo()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddInfoActuator();
            services.RemoveAll<IInfoContributor>();
        };

        var handler = testContext.GetRequiredService<IInfoEndpointHandler>();

        IDictionary<string, object> info = await handler.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(info);
        Assert.Empty(info);
    }

    [Fact]
    public async Task Invoke_CallsAllContributors()
    {
        using var testContext = new TestContext(_output);

        var contributors = new List<IInfoContributor>
        {
            new TestInfoContributor(),
            new TestInfoContributor(),
            new TestInfoContributor()
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddInfoActuator();
            services.AddSingleton<IEnumerable<IInfoContributor>>(contributors);
        };

        var handler = testContext.GetRequiredService<IInfoEndpointHandler>();

        await handler.InvokeAsync(null, CancellationToken.None);

        foreach (IInfoContributor contributor in contributors)
        {
            var testContributor = (TestInfoContributor)contributor;
            Assert.True(testContributor.Called);
        }
    }

    [Fact]
    public async Task Invoke_HandlesExceptions()
    {
        using var testContext = new TestContext(_output);

        var contributors = new List<IInfoContributor>
        {
            new TestInfoContributor(),
            new TestInfoContributor(true),
            new TestInfoContributor()
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddInfoActuator();
            services.AddSingleton<IEnumerable<IInfoContributor>>(contributors);
        };

        var handler = testContext.GetRequiredService<IInfoEndpointHandler>();

        await handler.InvokeAsync(null, CancellationToken.None);

        foreach (IInfoContributor contributor in contributors)
        {
            var testContributor = (TestInfoContributor)contributor;

            if (testContributor.Throws)
            {
                Assert.False(testContributor.Called);
            }
            else
            {
                Assert.True(testContributor.Called);
            }
        }
    }
}
