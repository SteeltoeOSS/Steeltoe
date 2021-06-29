// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.Common.Availability.Test
{
    public class ReadinessHealthContributorTest
    {
        private readonly ApplicationAvailability availability = new ApplicationAvailability();

        [Fact]
        public void HandlesUnknown()
        {
            // arrange
            var contributor = new ReadinessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.UNKNOWN, result.Status);
        }

        [Fact]
        public void HandlesAccepting()
        {
            // arrange
            availability.SetAvailabilityState(availability.ReadinessKey, ReadinessState.AcceptingTraffic, "tests");
            var contributor = new ReadinessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.UP, result.Status);
        }

        [Fact]
        public void HandlesRefusing()
        {
            // arrange
            availability.SetAvailabilityState(availability.ReadinessKey, ReadinessState.RefusingTraffic, "tests");
            var contributor = new ReadinessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.OUT_OF_SERVICE, result.Status);
        }
    }
}
