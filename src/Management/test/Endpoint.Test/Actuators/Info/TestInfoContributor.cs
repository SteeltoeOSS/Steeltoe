// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

internal sealed class TestInfoContributor(bool throws) : IInfoContributor
{
    public bool Throws { get; } = throws;
    public bool Called { get; private set; }

    public TestInfoContributor()
        : this(false)
    {
    }

    public Task ContributeAsync(IInfoBuilder builder, CancellationToken cancellationToken)
    {
        if (Throws)
        {
            throw new InvalidOperationException();
        }

        Called = true;
        return Task.CompletedTask;
    }
}
