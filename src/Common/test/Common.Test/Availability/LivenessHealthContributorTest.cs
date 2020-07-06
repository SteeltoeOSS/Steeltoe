// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.Common.Availability.Test
{
    public class LivenessHealthContributorTest
    {
        private readonly ApplicationAvailability availability = new ApplicationAvailability();

        [Fact]
        public void HandlesUnknown()
        {
            // arrange
            var contributor = new LivenessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.UNKNOWN, result.Status);
        }

        [Fact]
        public void HandlesCorrect()
        {
            // arrange
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Correct, "tests");
            var contributor = new LivenessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.UP, result.Status);
        }

        [Fact]
        public void HandlesBroken()
        {
            // arrange
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Broken, "tests");
            var contributor = new LivenessHealthContributor(availability);

            // act
            var result = contributor.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, result.Status);
        }
    }
}
