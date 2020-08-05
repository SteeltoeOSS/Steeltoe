// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthEndpointTest : BaseTest
    {
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
            var opts = new HealthEndpointOptions();
            var contributors = new List<IHealthContributor>();
            var agg = new DefaultHealthAggregator();
            var ep = new HealthEndpoint(opts, agg, contributors, GetLogger<HealthEndpoint>());

            var health = ep.Invoke(null);
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            var opts = new HealthEndpointOptions();
            var contributors = new List<IHealthContributor>() { new TestContrib("h1"), new TestContrib("h2"), new TestContrib("h3") };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contributors);

            var info = ep.Invoke(null);

            foreach (var contrib in contributors)
            {
                var tc = (TestContrib)contrib;
                Assert.True(tc.Called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions_ReturnsExpectedHealth()
        {
            var opts = new HealthEndpointOptions();
            var contributors = new List<IHealthContributor>() { new TestContrib("h1"), new TestContrib("h2", true), new TestContrib("h3") };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contributors);

            var info = ep.Invoke(null);

            foreach (var contrib in contributors)
            {
                var tc = (TestContrib)contrib;
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
            var opts = new HealthEndpointOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);

            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.DOWN }));
            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.OUT_OF_SERVICE }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UP }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UNKNOWN }));
        }

        [Fact]
        public void InvokeWithLivenessGroupReturnsGroupResults()
        {
            // arrange
            var opts = new HealthEndpointOptions();
            var appAvailability = new ApplicationAvailability();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor(), new LivenessHealthContributor(appAvailability) };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new string[] { "cloudfoundryapplication", "health", "liVeness" });
            appAvailability.SetAvailabilityState(appAvailability.LivenessKey, LivenessState.Correct, null);

            // act
            var result = ep.Invoke(context);

            // assert
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.True(result.Details.Keys.Count == 1);
            Assert.True(result.Groups.Count() == 2);
        }

        [Fact]
        public void InvokeWithReadinessGroupReturnsGroupResults()
        {
            // arrange
            var opts = new HealthEndpointOptions();
            var appAvailability = new ApplicationAvailability();
            var contribs = new List<IHealthContributor>() { new UnknownContributor(), new UpContributor(), new ReadinessHealthContributor(appAvailability) };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new string[] { "actuator", "health", "readiness" });
            appAvailability.SetAvailabilityState(appAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

            // act
            var result = ep.Invoke(context);

            // assert
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.True(result.Details.Keys.Count == 1);
            Assert.True(result.Groups.Count() == 2);
        }

        [Fact]
        public void InvokeWithInvalidGroupReturnsAllContributors()
        {
            // arrange
            var opts = new HealthEndpointOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor(), new OutOfSserviceContributor(), new UnknownContributor(), new UpContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);
            var context = Substitute.For<ISecurityContext>();
            context.GetRequestComponents().Returns(new string[] { "health", "iNvAlId" });

            // act
            var result = ep.Invoke(context);

            // assert
            Assert.Equal(HealthStatus.OUT_OF_SERVICE, result.Status);
            Assert.True(result.Details.Keys.Count == 4);
            Assert.True(result.Groups.Count() == 2);
        }
    }
}