// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health.Availability;

public sealed class ApplicationAvailabilityTest
{
    private int _livenessChanges;
    private LivenessState? _lastLivenessState;

    private int _readinessChanges;
    private ReadinessState? _lastReadinessState;

    [Fact]
    public void TracksAndReturnsState()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        availability.SetAvailabilityState("Test", LivenessState.Broken, GetType().Name);
        availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, GetType().Name);
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, GetType().Name);

        availability.GetAvailabilityState("Test").Should().Be(LivenessState.Broken);
        availability.GetLivenessState().Should().Be(LivenessState.Correct);
        availability.GetReadinessState().Should().Be(ReadinessState.AcceptingTraffic);
    }

    [Fact]
    public void ReturnsNullOnInit()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        AvailabilityState? liveness = availability.GetLivenessState();
        AvailabilityState? readiness = availability.GetReadinessState();

        liveness.Should().BeNull();
        readiness.Should().BeNull();
    }

    [Fact]
    public void KnownTypesRequireMatchingType()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        Action action1 = () => availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, ReadinessState.AcceptingTraffic, null);

        action1.Should().ThrowExactly<InvalidOperationException>();

        Action action2 = () => availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, LivenessState.Correct, null);

        action2.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void FiresEventsOnKnownTypeStateChange()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);
        availability.LivenessChanged += Availability_LivenessChanged;
        availability.ReadinessChanged += Availability_ReadinessChanged;

        availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Broken, null);
        _lastLivenessState.Should().Be(LivenessState.Broken);
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, null);
        _lastReadinessState.Should().Be(ReadinessState.RefusingTraffic);
        availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        _livenessChanges.Should().Be(2);
        _lastLivenessState.Should().Be(LivenessState.Correct);
        _readinessChanges.Should().Be(2);
        _lastReadinessState.Should().Be(ReadinessState.AcceptingTraffic);
    }

    private void Availability_ReadinessChanged(object? sender, AvailabilityEventArgs args)
    {
        _readinessChanges++;
        _lastReadinessState = (ReadinessState)args.NewState;
    }

    private void Availability_LivenessChanged(object? sender, AvailabilityEventArgs args)
    {
        _livenessChanges++;
        _lastLivenessState = (LivenessState)args.NewState;
    }
}
