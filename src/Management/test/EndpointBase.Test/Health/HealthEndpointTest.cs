// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Health.Test;

public class HealthEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public HealthEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ThrowsOptionsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(null, null, null));
        Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(new HealthEndpointOptions(), null, null));
        Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(new HealthEndpointOptions(), new DefaultHealthAggregator(), null));
    }

    [Fact]
    public void Invoke_NoContributors_ReturnsExpectedHealth()
    {
        using var tc = new TestContext(_output);
        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton(new List<IHealthContributor>());
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        var health = ep.Invoke(null);
        Assert.NotNull(health);
        Assert.Equal(HealthStatus.Unknown, health.Status);
    }

    [Fact]
    public void Invoke_CallsAllContributors()
    {
        using var tc = new TestContext(_output);
        var contributors = new List<IHealthContributor> { new TestContrib("h1"), new TestContrib("h2"), new TestContrib("h3") };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();
        ep.Invoke(null);

        foreach (var contrib in contributors)
        {
            var tcc = (TestContrib)contrib;
            Assert.True(tcc.Called);
        }
    }

    [Fact]
    public void Invoke_HandlesExceptions_ReturnsExpectedHealth()
    {
        using var tc = new TestContext(_output);
        var contributors = new List<IHealthContributor> { new TestContrib("h1"), new TestContrib("h2"), new TestContrib("h3") };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        var info = ep.Invoke(null);

        foreach (var contrib in contributors)
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
        var contributors = new List<IHealthContributor> { new DiskSpaceContributor() };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.Down }));
        Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.OutOfService }));
        Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.Up }));
        Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.Unknown }));
    }

    [Fact]
    public void InvokeWithLivenessGroupReturnsGroupResults()
    {
        using var tc = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();
        var contributors = new List<IHealthContributor> { new DiskSpaceContributor(), new LivenessHealthContributor(appAvailability) };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        var context = Substitute.For<ISecurityContext>();
        context.GetRequestComponents().Returns(new[] { "cloudfoundryapplication", "health", "liVeness" });
        appAvailability.SetAvailabilityState(appAvailability.LivenessKey, LivenessState.Correct, null);

        var result = ep.Invoke(context);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.True(result.Details.Keys.Count == 1);
        Assert.True(result.Groups.Count() == 2);
    }

    [Fact]
    public void InvokeWithReadinessGroupReturnsGroupResults()
    {
        using var tc = new TestContext(_output);
        var appAvailability = new ApplicationAvailability();
        var contributors = new List<IHealthContributor> { new UnknownContributor(), new UpContributor(), new ReadinessHealthContributor(appAvailability) };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        var context = Substitute.For<ISecurityContext>();
        context.GetRequestComponents().Returns(new[] { "actuator", "health", "readiness" });
        appAvailability.SetAvailabilityState(appAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        var result = ep.Invoke(context);

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.True(result.Details.Keys.Count == 1);
        Assert.True(result.Groups.Count() == 2);
    }

    [Fact]
    public void InvokeWithInvalidGroupReturnsAllContributors()
    {
        using var tc = new TestContext(_output);
        var contributors = new List<IHealthContributor> { new DiskSpaceContributor(), new OutOfServiceContributor(), new UnknownContributor(), new UpContributor() };

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IEnumerable<IHealthContributor>>(contributors);
            services.AddSingleton<IHealthAggregator>(new DefaultHealthAggregator());
            services.AddHealthActuatorServices(configuration);
        };

        var ep = tc.GetService<IHealthEndpoint>();

        var context = Substitute.For<ISecurityContext>();
        context.GetRequestComponents().Returns(new[] { "health", "iNvAlId" });

        var result = ep.Invoke(context);

        Assert.Equal(HealthStatus.OutOfService, result.Status);
        Assert.True(result.Details.Keys.Count == 4);
        Assert.True(result.Groups.Count() == 2);
    }
}
