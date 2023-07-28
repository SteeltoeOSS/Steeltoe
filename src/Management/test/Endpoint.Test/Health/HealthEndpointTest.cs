// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Test.Health.TestContributors;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using MicrosoftHealth = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health;

public sealed class HealthEndpointTest : BaseTest
{
    private readonly IHealthAggregator _aggregator = new DefaultHealthAggregator();
    private readonly ServiceProvider _provider = new ServiceCollection().BuildServiceProvider();
    private readonly ITestOutputHelper _output;

    public HealthEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_NoContributors_ReturnsExpectedHealth()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(new List<IHealthContributor>());
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task Invoke_CallsAllContributors()
    {
        using var testContext = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new TestContributors.TestContributor("h1"),
            new TestContributors.TestContributor("h2"),
            new TestContributors.TestContributor("h3")
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        HealthEndpointRequest healthRequest = GetHealthRequest();
        await handler.InvokeAsync(healthRequest, CancellationToken.None);

        foreach (IHealthContributor contributor in contributors)
        {
            var testContributor = (TestContributors.TestContributor)contributor;
            Assert.True(testContributor.Called);
        }
    }

    [Fact]
    public async Task Invoke_HandlesExceptions_ReturnsExpectedHealth()
    {
        using var testContext = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new TestContributors.TestContributor("h1"),
            new TestContributors.TestContributor("h2"),
            new TestContributors.TestContributor("h3")
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();
        HealthEndpointResponse info = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        foreach (IHealthContributor contributor in contributors)
        {
            var testContributor = (TestContributors.TestContributor)contributor;

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
        using var testContext = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor()
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = (HealthEndpointHandler)testContext.GetRequiredService<IHealthEndpointHandler>();

        Assert.Equal(503, handler.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Down
        }));

        Assert.Equal(503, handler.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.OutOfService
        }));

        Assert.Equal(200, handler.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Up
        }));

        Assert.Equal(200, handler.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Unknown
        }));
    }

    [Fact]
    public async Task InvokeWithLivenessGroupReturnsGroupResults()
    {
        using var testContext = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(),
            new LivenessHealthContributor(appAvailability)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);

        var healthRequest = new HealthEndpointRequest
        {
            GroupName = "liVeness",
            HasClaim = true
        };

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.True(result.Groups.Count() == 2);
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults()
    {
        using var testContext = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new UpContributor(),
            new ReadinessHealthContributor(appAvailability)
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var healthRequest = new HealthEndpointRequest
        {
            GroupName = "readiness",
            HasClaim = true
        };

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.True(result.Details.Keys.Count == 1);
        Assert.True(result.Groups.Count() == 2);
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults2()
    {
        var appAvailability = new ApplicationAvailability();

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new UpContributor(),
            new ReadinessHealthContributor(appAvailability)
        };

        IOptionsMonitor<HealthEndpointOptions> options = GetOptionsMonitorFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>();

        var handler = new HealthEndpointHandler(options, _aggregator, contributors, ServiceProviderWithMicrosoftHealth(), _provider,
            NullLoggerFactory.Instance);

        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var healthRequest = new HealthEndpointRequest
        {
            GroupName = "readiness",
            HasClaim = true
        };

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.Equal(2, result.Groups.Count());
    }

    [Fact]
    public async Task InvokeWithInvalidGroupReturnsAllContributors()
    {
        using var testContext = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new UpContributor()
        };

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        var healthRequest = new HealthEndpointRequest
        {
            GroupName = "iNvaLid",
            HasClaim = true
        };

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(HealthStatus.OutOfService, result.Status);
        Assert.True(result.Details.Keys.Count == 4);
        Assert.True(result.Groups.Count() == 2);
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
            new UpContributor()
        };

        var handler = new HealthEndpointHandler(options, new HealthRegistrationsAggregator(), contributors, ServiceProviderWithMicrosoftHealth(), _provider,
            NullLoggerFactory.Instance);

        var healthRequest = new HealthEndpointRequest
        {
            GroupName = "msft",
            HasClaim = true
        };

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(2, result.Details.Keys.Count);
        Assert.Contains("Up", result.Details.Keys);
        Assert.Contains("privatememory", result.Details.Keys);
        Assert.Equal(3, result.Groups.Count());
    }

    private static HealthEndpointRequest GetHealthRequest()
    {
        return new HealthEndpointRequest
        {
            GroupName = string.Empty,
            HasClaim = true
        };
    }

    private IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions> ServiceProviderWithMicrosoftHealth()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddPrivateMemoryHealthCheck(133_824_512).AddWorkingSetHealthCheck(133_824_512);

        return services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions>>();
    }
}
