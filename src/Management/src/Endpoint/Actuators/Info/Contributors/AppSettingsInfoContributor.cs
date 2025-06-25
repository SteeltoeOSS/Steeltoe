// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

internal sealed class AppSettingsInfoContributor : ConfigurationContributor, IInfoContributor
{
    private const string ConfigurationKey = "Info";

    public AppSettingsInfoContributor(IConfiguration configuration)
        : base(configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
    }

    public Task ContributeAsync(InfoBuilder builder, CancellationToken cancellationToken)
    {
        Contribute(builder, ConfigurationKey, false);
        return Task.CompletedTask;
    }
}
