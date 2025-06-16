// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info.TestContributors;

internal sealed class FakeContributor : IInfoContributor
{
    public Task ContributeAsync(InfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithInfo("TestKey", "TestValue");
        builder.WithInfo("TestTime", 19.July(2021).At(3, 41, 55, 3).AsUtc());

        return Task.CompletedTask;
    }
}
