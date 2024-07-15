// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Health.Availability;

public abstract class AvailabilityState
{
    private readonly string _value;

    protected AvailabilityState(string value)
    {
        ArgumentGuard.NotNull(value);

        _value = value;
    }

    public override string ToString()
    {
        return _value;
    }
}
