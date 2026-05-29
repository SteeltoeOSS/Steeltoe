// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

internal sealed class RuntimeInfoContributor : IInfoContributor
{
    public Task ContributeAsync(InfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithInfo("runtime", new Dictionary<string, string?>
        {
            ["runtimeName"] = RuntimeInformation.FrameworkDescription,
            ["runtimeVersion"] = System.Environment.Version.ToString(),
            ["runtimeIdentifier"] = RuntimeInformation.RuntimeIdentifier,
            ["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["osArchitecture"] = RuntimeInformation.OSArchitecture.ToString(),
            ["osDescription"] = RuntimeInformation.OSDescription,
            ["osVersion"] = System.Environment.OSVersion.ToString()
        });

        return Task.CompletedTask;
    }
}
