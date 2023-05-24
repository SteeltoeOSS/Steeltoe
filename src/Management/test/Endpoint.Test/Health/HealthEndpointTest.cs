// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Test.Health.MockContributors;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;
using MicrosoftHealth = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Test.Health;

public class HealthEndpointTest : BaseTest
{
    private readonly IOptionsMonitor<HealthEndpointOptions> _options = new TestOptionsMonitor<HealthEndpointOptions>(new HealthEndpointOptions());
    private readonly IHealthAggregator _aggregator = new DefaultHealthAggregator();
    private readonly ServiceProvider _provider = new ServiceCollection().BuildServiceProvider();
    private readonly ITestOutputHelper _output;

    public HealthEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsOnNulls()
    {
        var contributors = new List<IHealthContributor>();
        var nullLoggerFactory = NullLoggerFactory.Instance;

        var optionsEx = Assert.Throws<ArgumentNullException>(() =>
            new HealthEndpointHandler(null, _aggregator, contributors, ServiceOptions(), _provider, nullLoggerFactory));

        var aggregatorEx = Assert.Throws<ArgumentNullException>(() =>
            new HealthEndpointHandler(_options, null, contributors, ServiceOptions(), _provider, nullLoggerFactory));

        var contribEx = Assert.Throws<ArgumentNullException>(() =>
            new HealthEndpointHandler(_options, _aggregator, null, ServiceOptions(), _provider, nullLoggerFactory));

        var svcOptsEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointHandler(_options, _aggregator, contributors, null, _provider, nullLoggerFactory));

        var providerEx = Assert.Throws<ArgumentNullException>(() =>
            new HealthEndpointHandler(_options, _aggregator, contributors, ServiceOptions(), null, nullLoggerFactory));

        Assert.Equal("options", optionsEx.ParamName);
        Assert.Equal("aggregator", aggregatorEx.ParamName);
        Assert.Equal("contributors", contribEx.ParamName);
        Assert.Equal("serviceOptions", svcOptsEx.ParamName);
        Assert.Equal("provider", providerEx.ParamName);
    }

    [Fact]
    public async Task Invoke_NoContributors_ReturnsExpectedHealth()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton(new List<IHealthContributor>());
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();

        HealthEndpointResponse health = await ep.InvokeAsync(null, CancellationToken.None);
        Assert.NotNull(health);
        Assert.Equal(HealthStatus.Unknown, health.Status);
    }

    [Fact]
    public async Task Invoke_CallsAllContributors()
    {
        using var tc = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new TestContrib("h1"),
            new TestContrib("h2"),
            new TestContrib("h3")
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();
        await ep.InvokeAsync(null, CancellationToken.None);

        foreach (IHealthContributor contrib in contributors)
        {
            var tcc = (TestContrib)contrib;
            Assert.True(tcc.Called);
        }
    }

    [Fact]
    public async Task Invoke_HandlesExceptions_ReturnsExpectedHealth()
    {
        using var tc = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new TestContrib("h1"),
            new TestContrib("h2"),
            new TestContrib("h3")
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();

        HealthEndpointResponse info = await ep.InvokeAsync(null, CancellationToken.None);

        foreach (IHealthContributor contrib in contributors)
        {
            var tcc = (TestContrib)contrib;

            if (tcc.Throws)
            {
                Assert.False(tcc.Called);
            }
            else
            {
                Assert.True(tcc.Called);
            }
        }

        Assert.Equal(HealthStatus.Up, info.Status);
    }

    [Fact]
    public void GetStatusCode_ReturnsExpected()
    {
        using var tc = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor()
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = (HealthEndpointHandler)tc.GetService<IHealthEndpointHandler>();

        Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Down
        }));

        Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.OutOfService
        }));

        Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Up
        }));

        Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult
        {
            Status = HealthStatus.Unknown
        }));
    }

    [Fact]
    public async Task InvokeWithLivenessGroupReturnsGroupResults()
    {
        using var tc = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(),
            new LivenessHealthContributor(appAvailability)
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();

        var healthRequest = Substitute.For<HealthEndpointRequest>();

        healthRequest.GroupName.Returns(string.Empty);
        healthRequest.HasClaim.Returns(true);

        appAvailability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);
        var request = new HealthEndpointRequest(); // How to get?
        HealthEndpointResponse result = await ep.InvokeAsync(request, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.True(result.Details.Keys.Count == 1);
        Assert.True(result.Groups.Count() == 2);
    }

    [Fact]
    public async Task InvokeWithReadinessGroupReturnsGroupResults()
    {
        using var tc = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();

        var contributors = new List<IHealthContributor>
        {
            new UnknownContributor(),
            new UpContributor(),
            new ReadinessHealthContributor(appAvailability)
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();
        var healthRequest = Substitute.For<HealthEndpointRequest>();

        //var context = Substitute.For<ISecurityContext>();

        //context.GetRequestComponents().Returns(new[]
        //{
        //    "actuator",
        //    "health",
        //    "readiness"
        //});
        healthRequest.GroupName.Returns(string.Empty);
        healthRequest.HasClaim.Returns(true);

        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);
        var request = new HealthEndpointRequest();
        HealthEndpointResponse result = await ep.InvokeAsync(request, CancellationToken.None);

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

        var ep = new HealthEndpointHandler(options, _aggregator, contributors, ServiceProviderWithMicrosoftHealth(), _provider, NullLoggerFactory.Instance);

        //var context = Substitute.For<ISecurityContext>();

        //context.GetRequestComponents().Returns(new[]
        //{
        //    "actuator",
        //    "health",
        //    "readiness"
        //});

        var healthRequest = Substitute.For<HealthEndpointRequest>();
        healthRequest.GroupName.Returns(string.Empty);
        healthRequest.HasClaim.Returns(true);

        appAvailability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);
        var request = new HealthEndpointRequest();
        HealthEndpointResponse result = await ep.InvokeAsync(request, CancellationToken.None);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Single(result.Details.Keys);
        Assert.Equal(2, result.Groups.Count());
    }

    [Fact]
    public async Task InvokeWithInvalidGroupReturnsAllContributors()
    {
        using var tc = new TestContext(_output);

        var contributors = new List<IHealthContributor>
        {
            new DiskSpaceContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new UpContributor()
        };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices();
        };

        var ep = tc.GetService<IHealthEndpointHandler>();
    

        var request = Substitute.For<HealthEndpointRequest>();
        request.GroupName.Returns(string.Empty);
        request.HasClaim.Returns(true);

        HealthEndpointResponse result = await ep.InvokeAsync(request, CancellationToken.None);

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

        var ep = new HealthEndpointHandler(options, new HealthRegistrationsAggregator(), contributors, ServiceProviderWithMicrosoftHealth(), _provider,
            NullLoggerFactory.Instance);

        //var context = Substitute.For<ISecurityContext>();

        //context.GetRequestComponents().Returns(new[]
        //{
        //    "actuator",
        //    "health",
        //    "msft"
        //});
        var healthRequest = Substitute.For<HealthEndpointRequest>();


        HealthEndpointResponse result = await ep.InvokeAsync(healthRequest, CancellationToken.None);

        Assert.Equal(2, result.Details.Keys.Count);
        Assert.Contains("Up", result.Details.Keys);
        Assert.Contains("privatememory", result.Details.Keys);
        Assert.Equal(3, result.Groups.Count());
    }

    private IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions> ServiceOptions()
    {
        var options = Substitute.For<IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions>>();
        options.CurrentValue.Returns(new MicrosoftHealth.HealthCheckServiceOptions());
        return options;
    }

    private IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions> ServiceProviderWithMicrosoftHealth()
    {
        var services = new ServiceCollection();
        services.AddHealthChecks().AddPrivateMemoryHealthCheck(133_824_512).AddWorkingSetHealthCheck(133_824_512);

        return services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<MicrosoftHealth.HealthCheckServiceOptions>>();
    }
}
