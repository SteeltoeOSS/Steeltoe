// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Availability;

public class AvailabilityEventArgs : EventArgs
{
    public IAvailabilityState NewState { get; }

    public AvailabilityEventArgs(IAvailabilityState availabilityState)
    {
        NewState = availabilityState;
    }
}
