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
            var contributor = new LivenessHealthContributor(availability);

            var result = contributor.Health();

            Assert.Equal(HealthStatus.UNKNOWN, result.Status);
        }

        [Fact]
        public void HandlesCorrect()
        {
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Correct, "tests");
            var contributor = new LivenessHealthContributor(availability);

            var result = contributor.Health();

            Assert.Equal(HealthStatus.UP, result.Status);
        }

        [Fact]
        public void HandlesBroken()
        {
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Broken, "tests");
            var contributor = new LivenessHealthContributor(availability);

            var result = contributor.Health();

            Assert.Equal(HealthStatus.DOWN, result.Status);
        }
    }
}
