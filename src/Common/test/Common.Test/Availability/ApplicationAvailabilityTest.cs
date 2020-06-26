// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using Xunit;

namespace Steeltoe.Common.Availability.Test
{
    public class ApplicationAvailabilityTest
    {
        private ILogger<ApplicationAvailability> _logger = TestHelpers.GetLoggerFactory().CreateLogger<ApplicationAvailability>();

        [Fact]
        public void TracksAndReturnsState()
        {
            // arrange
            var availability = new ApplicationAvailability(_logger);

            // act
            availability.SetAvailabilityState("Test", LivenessState.Broken, GetType().Name);
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Correct, GetType().Name);
            availability.SetAvailabilityState(availability.ReadinessKey, ReadinessState.AcceptingTraffic, GetType().Name);

            // assert
            Assert.Equal(LivenessState.Broken, availability.GetAvailabilityState("Test"));
            Assert.Equal(LivenessState.Correct, availability.GetLivenessState());
            Assert.Equal(ReadinessState.AcceptingTraffic, availability.GetReadinessState());
        }

        [Fact]
        public void ReturnsNullOnInit()
        {
            // arrange
            var availability = new ApplicationAvailability(_logger);

            // act
            var liveness = availability.GetLivenessState();
            var readiness = availability.GetReadinessState();

            // assert
            Assert.Null(liveness);
            Assert.Null(readiness);
        }

        [Fact]
        public void KnownTypesRequireMatchingType()
        {
            // arrange
            var availability = new ApplicationAvailability(_logger);

            // act
            Assert.Throws<InvalidOperationException>(() => availability.SetAvailabilityState(availability.LivenessKey, ReadinessState.AcceptingTraffic, null));
            Assert.Throws<InvalidOperationException>(() => availability.SetAvailabilityState(availability.ReadinessKey, LivenessState.Correct, null));
        }
    }
}
