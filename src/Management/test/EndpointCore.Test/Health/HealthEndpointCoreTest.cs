// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using MSFTHealth = Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthEndpointCoreTest : BaseTest
    {
        private readonly IHealthOptions options = new HealthEndpointOptions();
        private readonly IHealthAggregator aggregator = new DefaultHealthAggregator();
        private readonly ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        [Fact]
        public void Constructor_ThrowsOnNulls()
        {
            var contributors = new List<IHealthContributor>();

            var optionsEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(null, aggregator, contributors, ServiceOptions(), provider));
            var aggrgtrEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(options, null, contributors, ServiceOptions(), provider));
            var contribEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(options, aggregator, null, ServiceOptions(), provider));
            var svcOptsEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(options, aggregator, contributors, null, provider));
            var svcPrvdEx = Assert.Throws<ArgumentNullException>(() => new HealthEndpointCore(options, aggregator, contributors, ServiceOptions(), null));

            Assert.Equal(nameof(options), optionsEx.ParamName);
            Assert.Equal(nameof(aggregator), aggrgtrEx.ParamName);
            Assert.Equal(nameof(contributors), contribEx.ParamName);
            Assert.Equal("serviceOptions", svcOptsEx.ParamName);
            Assert.Equal(nameof(provider), svcPrvdEx.ParamName);
        }

        [Fact]
        public void Invoke_NoContributors_ReturnsExpectedHealth()
        {
            var contributors = new List<IHealthContributor>();
            var ep = new HealthEndpointCore(options, aggregator, contributors, ServiceOptions(), provider, GetLogger<HealthEndpoint>());

            var health = ep.Invoke(null);
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            var contributors = new List<IHealthContributor>() { new TestContributor("h1"), new TestContributor("h2"), new TestContributor("h3") };
            var ep = new HealthEndpointCore(options, aggregator, contributors, ServiceOptions(), provider);

            var info = ep.Invoke(null);

            foreach (var contrib in contributors)
            {
                var tc = (TestContributor)contrib;
                Assert.True(tc.Called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions_ReturnsExpectedHealth()
        {
            var contributors = new List<IHealthContributor>() { new TestContributor("h1"), new TestContributor("h2", true), new TestContributor("h3") };
            var ep = new HealthEndpointCore(options, aggregator, contributors, ServiceOptions(), provider);

            var info = ep.Invoke(null);

            foreach (var contrib in contributors)
            {
                var tc = (TestContributor)contrib;
                if (tc.Throws)
                {
                    Assert.False(tc.Called);
                }
                else
                {
                    Assert.True(tc.Called);
                }
            }

            Assert.Equal(HealthStatus.UP, info.Status);
        }

        [Fact]
        public void GetStatusCode_ReturnsExpected()
        {
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpointCore(options, aggregator, contribs, ServiceOptions(), provider);

            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.DOWN }));
            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.OUT_OF_SERVICE }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UP }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UNKNOWN }));
        }

        [Fact]
        public void InvokeWithInvalidGroupReturnsAllContributors()
        {
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor(), new OutOfSserviceContributor(), new UnknownContributor(), new UpContributor() };
            var ep = new HealthEndpointCore(options, new HealthRegistrationsAggregator(), contribs, ServiceProviderWithMSFTHealth(), provider);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new[] { "health", "iNvAlId" });

            var result = ep.Invoke(context);

            Assert.Equal(6, result.Details.Keys.Count);
            Assert.Equal(2, result.Groups.Count());
        }

        [Fact]
        public void InvokeWithLivenessGroupReturnsGroupResults()
        {
            var appAvailability = new ApplicationAvailability();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor(), new LivenessHealthContributor(appAvailability) };
            var ep = new HealthEndpointCore(options, aggregator, contribs, ServiceOptions(), provider);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new[] { "cloudfoundryapplication", "health", "liVeness" });
            appAvailability.SetAvailabilityState(appAvailability.LivenessKey, LivenessState.Correct, null);

            var result = ep.Invoke(context);

            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Single(result.Details.Keys);
            Assert.Equal(2, result.Groups.Count());
        }

        [Fact]
        public void InvokeWithReadinessGroupReturnsGroupResults()
        {
            var appAvailability = new ApplicationAvailability();
            var contribs = new List<IHealthContributor>() { new UnknownContributor(), new UpContributor(), new ReadinessHealthContributor(appAvailability) };
            var ep = new HealthEndpointCore(options, aggregator, contribs, ServiceOptions(), provider);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new[] { "actuator", "health", "readiness" });
            appAvailability.SetAvailabilityState(appAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

            var result = ep.Invoke(context);

            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Single(result.Details.Keys);
            Assert.Equal(2, result.Groups.Count());
        }

        [Fact]
        public void InvokeWithReadinessGroupReturnsGroupResults2()
        {
            var appAvailability = new ApplicationAvailability();
            var contribs = new List<IHealthContributor>() { new UnknownContributor(), new UpContributor(), new ReadinessHealthContributor(appAvailability) };
            var ep = new HealthEndpointCore(options, aggregator, contribs, ServiceProviderWithMSFTHealth(), provider);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new[] { "actuator", "health", "readiness" });
            appAvailability.SetAvailabilityState(appAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

            var result = ep.Invoke(context);

            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Single(result.Details.Keys);
            Assert.Equal(2, result.Groups.Count());
        }

        [Fact]
        public void InvokeWithGroupFiltersMSFTResults()
        {
            options.Groups.Add("msft", new HealthGroupOptions { Include = "up,privatememory" });
            var contribs = new List<IHealthContributor>() { new UnknownContributor(), new UpContributor() };
            var ep = new HealthEndpointCore(options, new HealthRegistrationsAggregator(), contribs, ServiceProviderWithMSFTHealth(), provider);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new[] { "actuator", "health", "msft" });

            var result = ep.Invoke(context);

            Assert.Equal(2, result.Details.Keys.Count);
            Assert.Contains("Up", result.Details.Keys);
            Assert.Contains("privatememory", result.Details.Keys);
            Assert.Equal(3, result.Groups.Count());
        }

        private IOptionsMonitor<MSFTHealth.HealthCheckServiceOptions> ServiceOptions()
        {
            var options = Substitute.For<IOptionsMonitor<MSFTHealth.HealthCheckServiceOptions>>();
            options.CurrentValue.Returns(new MSFTHealth.HealthCheckServiceOptions());
            return options;
        }

        private IOptionsMonitor<MSFTHealth.HealthCheckServiceOptions> ServiceProviderWithMSFTHealth()
        {
            var services = new ServiceCollection();
            services.AddHealthChecks()
                .AddPrivateMemoryHealthCheck(133824512)
                .AddWorkingSetHealthCheck(133824512);

            return services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<MSFTHealth.HealthCheckServiceOptions>>();
        }
    }
}
