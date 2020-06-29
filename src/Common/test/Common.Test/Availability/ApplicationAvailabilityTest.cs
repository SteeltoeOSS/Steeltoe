﻿// Licensed to the .NET Foundation under one or more agreements.
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

        [Fact]
        public void FiresEventsOnKnownTypeStateChange()
        {
            // arrange
            var availability = new ApplicationAvailability(_logger);
            availability.LivenessChanged += Availability_LivenessChanged;
            availability.ReadinessChanged += Availability_ReadinessChanged;

            // act
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Broken, null);
            Assert.Equal(LivenessState.Broken, lastLivenessState);
            availability.SetAvailabilityState(availability.ReadinessKey, ReadinessState.RefusingTraffic, null);
            Assert.Equal(ReadinessState.RefusingTraffic, lastReadinessState);
            availability.SetAvailabilityState(availability.LivenessKey, LivenessState.Correct, null);
            availability.SetAvailabilityState(availability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

            // assert
            Assert.Equal(2, livenessChanges);
            Assert.Equal(LivenessState.Correct, lastLivenessState);
            Assert.Equal(2, readinessChanges);
            Assert.Equal(ReadinessState.AcceptingTraffic, lastReadinessState);
        }

        private int livenessChanges = 0;
        private LivenessState lastLivenessState;

        private int readinessChanges = 0;
        private ReadinessState lastReadinessState;

        private void Availability_ReadinessChanged(object sender, EventArgs e)
        {
            readinessChanges++;
            lastReadinessState = (ReadinessState)(e as AvailabilityEventArgs).NewState;
        }

        private void Availability_LivenessChanged(object sender, EventArgs e)
        {
            livenessChanges++;
            lastLivenessState = (LivenessState)(e as AvailabilityEventArgs).NewState;
        }
    }
}
