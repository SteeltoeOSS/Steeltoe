// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors.FileSystem;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using MicrosoftHealth = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private static readonly IServiceProvider EmptyServiceProvider = new ServiceCollection().BuildServiceProvider(true);
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_NoContributors_ReturnsExpectedHealth()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton(new List<IHealthContributor>());
            services.AddHealthActuator();

            services.RemoveAll<IHealthContributor>();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task Invoke_CallsAllContributors()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        List<IHealthContributor> contributors =
        [
            new TestHealthContributor("h1"),
            new TestHealthContributor("h2"),
            new TestHealthContributor("h3")
        ];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        HealthEndpointRequest healthRequest = GetHealthRequest();
        await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

        foreach (TestHealthContributor testContributor in contributors.Cast<TestHealthContributor>())
        {
            Assert.True(testContributor.Called);
        }
    }

    [Fact]
    public async Task Invoke_HandlesCancellation_Throws()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        List<IHealthContributor> contributors = [new UpContributor(5.Seconds())];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        HealthEndpointRequest healthRequest = GetHealthRequest();
        Func<Task> action = async () => await handler.InvokeAsync(healthRequest, source.Token);

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Invoke_HandlesExceptions_ReturnsExpectedHealth()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        List<IHealthContributor> contributors =
        [
            new TestHealthContributor("h1"),
            new TestHealthContributor("h2", true),
            new TestHealthContributor("h3", true)
        ];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        HealthEndpointRequest healthRequest = GetHealthRequest();
        HealthEndpointResponse info = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

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
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        List<IHealthContributor> contributors =
        [
            new DiskSpaceHealthContributor(new DiskSpaceProvider(), GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>())
        ];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = (HealthEndpointHandler)testContext.GetRequiredService<IHealthEndpointHandler>();

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
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Liveness:Enabled"] = "true"
        };

        testContext.AdditionalConfiguration = configuration => configuration.AddInMemoryCollection(appSettings);

        var appAvailability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        IOptionsMonitor<LivenessStateContributorOptions> optionsMonitor = GetOptionsMonitorFromSettings<LivenessStateContributorOptions>(appSettings);

        List<IHealthContributor> contributors =
        [
            new DiskSpaceHealthContributor(new DiskSpaceProvider(), GetOptionsMonitorFromSettings<DiskSpaceContributorOptions>()),
            new LivenessStateHealthContributor(appAvailability, optionsMonitor, NullLoggerFactory.Instance)
        ];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);

        var healthRequest = new HealthEndpointRequest("liVeness", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Up);
        result.Components.Keys.Should().ContainSingle();
        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:ShowComponents"] = "Always",
            ["Management:Endpoints:Health:Readiness:Enabled"] = "true"
        };

        testContext.AdditionalConfiguration = configuration =>
        {
            configuration.AddInMemoryCollection(appSettings);
        };

        var appAvailability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        IOptionsMonitor<ReadinessStateContributorOptions> optionsMonitor = GetOptionsMonitorFromSettings<ReadinessStateContributorOptions>(appSettings);

        List<IHealthContributor> contributors =
        [
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor(),
            new ReadinessStateHealthContributor(appAvailability, optionsMonitor, NullLoggerFactory.Instance)
        ];

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();
        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var healthRequest = new HealthEndpointRequest("readiness", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Up);
        result.Components.Keys.Should().ContainSingle();
        result.Groups.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeWithGroupFiltersMicrosoftResults()
    {
        IOptionsMonitor<HealthEndpointOptions> options = GetOptionsMonitorFromSettings<HealthEndpointOptions, ConfigureHealthEndpointOptions>(
            new Dictionary<string, string?>
            {
                ["Management:Endpoints:Health:ShowComponents"] = "Always"
            });

        options.CurrentValue.Groups.Add("msft", new HealthGroupOptions
        {
            Include = "alwaysUp,privatememory"
        });

        List<IHealthContributor> contributors =
        [
            new UnknownContributor(),
            new DisabledContributor(),
            new UpContributor()
        ];

        var handler = new HealthEndpointHandler(options, new HealthAggregator(), contributors, ServiceProviderWithMicrosoftHealth(), EmptyServiceProvider,
            NullLoggerFactory.Instance);

        var healthRequest = new HealthEndpointRequest("msft", true);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

        result.Components.Keys.Should().HaveCount(2);
        result.Components.Should().ContainKey("alwaysUp");
        result.Components.Should().ContainKey("privatememory");
        result.Groups.Should().BeEmpty();
    }

    // default configuration
    [InlineData(null, null, null, null, false, false, false)]
    // misconfiguration
    [InlineData(null, "Always", null, null, false, false, false)]
    [InlineData(null, "WhenAuthorized", null, null, false, false, false)]
    [InlineData(null, "WhenAuthorized", null, null, true, false, false)]
    [InlineData(null, null, null, "Always", false, false, false)]
    [InlineData(null, null, null, "WhenAuthorized", false, false, false)]
    [InlineData(null, null, null, "WhenAuthorized", true, false, false)]
    // only endpoint configuration
    [InlineData("Always", null, null, null, false, true, false)]
    [InlineData("WhenAuthorized", null, null, null, false, false, false)]
    [InlineData("WhenAuthorized", null, null, null, true, true, false)]
    [InlineData("Always", "Always", null, null, false, true, true)]
    [InlineData("Always", "WhenAuthorized", null, null, false, true, false)]
    [InlineData("Always", "WhenAuthorized", null, null, true, true, true)]
    [InlineData("WhenAuthorized", "Always", null, null, false, false, false)]
    [InlineData("WhenAuthorized", "Always", null, null, true, true, true)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, null, true, true, true)]
    // only group configuration
    [InlineData(null, null, "Always", null, false, true, false)]
    [InlineData(null, null, "WhenAuthorized", null, false, false, false)]
    [InlineData(null, null, "WhenAuthorized", null, true, true, false)]
    [InlineData(null, null, "Always", "Always", false, true, true)]
    [InlineData(null, null, "WhenAuthorized", "WhenAuthorized", false, false, false)]
    [InlineData(null, null, "WhenAuthorized", "WhenAuthorized", true, true, true)]
    // group supplements endpoint
    [InlineData("Always", null, null, "Always", true, true, true)]
    [InlineData("Always", null, null, "WhenAuthorized", false, true, false)]
    [InlineData("Always", null, null, "WhenAuthorized", true, true, true)]
    [InlineData("WhenAuthorized", null, null, "WhenAuthorized", false, false, false)]
    [InlineData("WhenAuthorized", null, null, "WhenAuthorized", true, true, true)]
    [InlineData(null, "Always", "Always", null, false, true, true)]
    [InlineData(null, "WhenAuthorized", "Always", null, false, true, false)]
    [InlineData(null, "WhenAuthorized", "Always", null, true, true, true)]
    [InlineData(null, "Always", "WhenAuthorized", null, false, false, false)]
    [InlineData(null, "Always", "WhenAuthorized", null, true, true, true)]
    // group overrides endpoint
    [InlineData("Never", "Never", "Always", null, false, true, false)]
    [InlineData("Never", "Never", "WhenAuthorized", null, false, false, false)]
    [InlineData("Never", "Never", "WhenAuthorized", null, true, true, false)]
    [InlineData("Never", "Never", "Always", "Always", false, true, true)]
    [InlineData("Never", "Never", "WhenAuthorized", "WhenAuthorized", false, false, false)]
    [InlineData("Never", "Never", "WhenAuthorized", "WhenAuthorized", true, true, true)]
    [InlineData("Always", null, "Never", null, false, false, false)]
    [InlineData("WhenAuthorized", null, "Never", null, false, false, false)]
    [InlineData("WhenAuthorized", null, "Never", null, true, false, false)]
    [InlineData("Always", null, "WhenAuthorized", null, false, false, false)]
    [InlineData("Always", null, "WhenAuthorized", null, true, true, false)]
    [InlineData("Always", "Always", "WhenAuthorized", null, false, false, false)]
    [InlineData("Always", "Always", "WhenAuthorized", null, true, true, true)]
    [InlineData("Always", "Always", null, "Never", false, true, false)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, "Never", false, false, false)]
    [InlineData("WhenAuthorized", "WhenAuthorized", null, "Never", true, true, false)]
    [Theory]
    public async Task InvokeWithGroupUsesShowComponentShowDetailOptions(string? endpointShowComponents, string? endpointShowDetails,
        string? groupShowComponents, string? groupShowDetails, bool hasClaim, bool returnComponents, bool returnDetails)
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalConfiguration = configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Management:Endpoints:Health:ShowComponents"] = endpointShowComponents,
            ["Management:Endpoints:Health:ShowDetails"] = endpointShowDetails,
            ["Management:Endpoints:Health:Groups:test:include"] = "diskSpace",
            ["Management:Endpoints:Health:Groups:test:ShowComponents"] = groupShowComponents,
            ["Management:Endpoints:Health:Groups:test:ShowDetails"] = groupShowDetails
        });

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHealthActuator();
        };

        var handler = testContext.GetRequiredService<IHealthEndpointHandler>();

        var healthRequest = new HealthEndpointRequest("test", hasClaim);

        HealthEndpointResponse result = await handler.InvokeAsync(healthRequest, TestContext.Current.CancellationToken);

        result.Status.Should().Be(HealthStatus.Up);

        if (returnComponents)
        {
            result.Components.Keys.Should().ContainSingle();

            if (returnDetails)
            {
                HealthCheckResult component = result.Components["diskSpace"];
                component.Should().NotBeNull();
                component.Details.Should().NotBeEmpty();
            }
        }
        else
        {
            result.Components.Keys.Should().BeEmpty();
        }

        result.Groups.Should().BeEmpty();
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
