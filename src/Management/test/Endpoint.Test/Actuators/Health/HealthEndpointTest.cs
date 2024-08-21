// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using Xunit.Abstractions;
using MicrosoftHealth = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private static readonly IServiceProvider EmptyServiceProvider = new ServiceCollection().BuildServiceProvider(true);
    private readonly IHealthAggregator _aggregator = new HealthAggregator();
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_NoContributors_ReturnsExpectedHealth()
    {
        using var testContext = new TestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(new List<IHealthContributor>());
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();

            services.RemoveAll<IHealthContributor>();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task Invoke_CallsAllContributors()
    {
        using var testContext = new TestContext(_testOutputHelper);

        var contributors = new List<IHealthContributor>
        {
            new TestHealthContributor("h1"),
            new TestHealthContributor("h2"),
            new TestHealthContributor("h3")
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();
        HealthEndpointRequest healthRequest = GetHealthRequest();
        await handler.InvokeAsync(healthRequest, CancellationToken.None);

        foreach (TestHealthContributor testContributor in contributors.Cast<TestHealthContributor>())
        {
            Assert.True(testContributor.Called);
        }
    }

    [Fact]
    public async Task Invoke_HandlesCancellation_Throws()
    {
        using var testContext = new TestContext(_testOutputHelper);

        var contributors = new List<IHealthContributor>
        {
            new UpContributor(5000)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        HealthEndpointRequest healthRequest = GetHealthRequest();
        Func<Task> action = async () => await handler.InvokeAsync(healthRequest, source.Token);

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Invoke_HandlesExceptions_ReturnsExpectedHealth()
    {
        using var testContext = new TestContext(_testOutputHelper);

        var contributors = new List<IHealthContributor>
        {
            new TestHealthContributor("h1"),
            new TestHealthContributor("h2", true),
            new TestHealthContributor("h3", true)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();
        HealthEndpointResponse info = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        foreach (TestHealthContributor testContributor in contributors.Cast<TestHealthContributor>())
        {
            if (testContributor.Throws)
            {
                Assert.False(testContributor.Called);
            }
            else
            {
                Assert.True(testContributor.Called);
            }
        }

        Assert.Equal(HealthStatus.Up, info.Status);
    }

    [Fact]
    public void GetStatusCode_ReturnsExpected()
    {
        using var testContext = new TestContext(_testOutputHelper);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>())
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = (HealthEndpointHandler)testContext.GetRequiredScopedService<IHealthEndpointHandler>();

        Assert.Equal(503, handler.GetStatusCode(new HealthEndpointResponse
        {
            Status = HealthStatus.Down
        }));

        Assert.Equal(503, handler.GetStatusCode(new HealthEndpointResponse
        {
            Status = HealthStatus.OutOfService
        }));

        Assert.Equal(200, handler.GetStatusCode(new HealthEndpointResponse
        {
            Status = HealthStatus.Up
        }));

        Assert.Equal(200, handler.GetStatusCode(new HealthEndpointResponse
        {
            Status = HealthStatus.Unknown
        }));
    }

    [Fact]
    public async Task InvokeWithLivenessGroupReturnsGroupResults()
    {
        using var testContext = new TestContext(_testOutputHelper);
        var appAvailability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>()),
            new LivenessHealthContributor(appAvailability, NullLoggerFactory.Instance)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);

        var healthRequest = new HealthEndpointRequest("liVeness", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.Equal(2, result.Groups.Count);
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults()
    {
        using var testContext = new TestContext(_testOutputHelper);
        var appAvailability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor(),
            new ReadinessHealthContributor(appAvailability, NullLoggerFactory.Instance)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var healthRequest = new HealthEndpointRequest("readiness", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.Equal(2, result.Groups.Count);
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults2()
    {
        var appAvailability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor(),
            new ReadinessHealthContributor(appAvailability, NullLoggerFactory.Instance)
        };

        IOptionsMonitor<HealthEndpointOptions> options = GetOptionsMonitorFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>();

        var handler = new HealthEndpointHandler(options, _aggregator, contributors, ServiceProviderWithMicrosoftHealth(), EmptyServiceProvider,
            NullLoggerFactory.Instance);

        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var healthRequest = new HealthEndpointRequest("readiness", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.Equal(2, result.Groups.Count);
    }

    [Fact]
    public async Task InvokeWithInvalidGroupReturnsAllContributors()
    {
        using var testContext = new TestContext(_testOutputHelper);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>()),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor()
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new HealthAggregator());
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredScopedService<IHealthEndpointHandler>();

        var healthRequest = new HealthEndpointRequest("iNvaLid", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.OutOfService, result.Status);
        Assert.Equal(4, result.Details.Keys.Count);
        Assert.Equal(2, result.Groups.Count);
    }

    [Fact]
    public async Task InvokeWithGroupFiltersMicrosoftResults()
    {
        IOptionsMonitor<HealthEndpointOptions> options = GetOptionsMonitorFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>();

        options.CurrentValue.Groups.Add("msft", new HealthGroupOptions
        {
            Include = "up,privatememory"
        });

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor()
        };

        var handler = new HealthEndpointHandler(options, new HealthAggregator(), contributors, ServiceProviderWithMicrosoftHealth(), EmptyServiceProvider,
            NullLoggerFactory.Instance);

        var healthRequest = new HealthEndpointRequest("msft", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(2, result.Details.Keys.Count);
        Assert.Contains("Up", result.Details.Keys);
        Assert.Contains("privatememory", result.Details.Keys);
        Assert.Equal(3, result.Groups.Count);
    }

    private static HealthEndpointRequest GetHealthRequest()
    {
        return new HealthEndpointRequest(string.Empty, true);
    }

    private IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions> ServiceProviderWithMicrosoftHealth()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddPrivateMemoryHealthCheck(133_824_512).AddWorkingSetHealthCheck(133_824_512);

        using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        return serviceProvider.GetRequiredService<IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions>>();
    }
}
