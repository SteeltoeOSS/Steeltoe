// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info.TestContributors;

internal sealed class ThrowingContributor : IInfoContributor
{
    public async Task ContributeAsync(InfoBuilder builder, CancellationToken cancellationToken)
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }
}
