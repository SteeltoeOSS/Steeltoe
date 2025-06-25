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

        Assert.Equal(LivenessState.Broken, availability.GetAvailabilityState("Test"));
        Assert.Equal(LivenessState.Correct, availability.GetLivenessState());
        Assert.Equal(ReadinessState.AcceptingTraffic, availability.GetReadinessState());
    }

    [Fact]
    public void ReturnsNullOnInit()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        AvailabilityState? liveness = availability.GetLivenessState();
        AvailabilityState? readiness = availability.GetReadinessState();

        Assert.Null(liveness);
        Assert.Null(readiness);
    }

    [Fact]
    public void KnownTypesRequireMatchingType()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);

        Assert.Throws<InvalidOperationException>(() =>
            availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, ReadinessState.AcceptingTraffic, null));

        Assert.Throws<InvalidOperationException>(() => availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, LivenessState.Correct, null));
    }

    [Fact]
    public void FiresEventsOnKnownTypeStateChange()
    {
        var availability = new ApplicationAvailability(NullLogger<ApplicationAvailability>.Instance);
        availability.LivenessChanged += Availability_LivenessChanged;
        availability.ReadinessChanged += Availability_ReadinessChanged;

        availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Broken, null);
        Assert.Equal(LivenessState.Broken, _lastLivenessState);
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, null);
        Assert.Equal(ReadinessState.RefusingTraffic, _lastReadinessState);
        availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, null);
        availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, null);

        Assert.Equal(2, _livenessChanges);
        Assert.Equal(LivenessState.Correct, _lastLivenessState);
        Assert.Equal(2, _readinessChanges);
        Assert.Equal(ReadinessState.AcceptingTraffic, _lastReadinessState);
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
