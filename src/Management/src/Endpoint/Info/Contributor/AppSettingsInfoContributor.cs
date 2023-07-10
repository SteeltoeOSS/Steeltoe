// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info.Contributor;

internal sealed class AppSettingsInfoContributor : AbstractConfigurationContributor, IInfoContributor
{
    private const string AppsettingsPrefix = "info";

    public AppSettingsInfoContributor(IConfiguration configuration)
        : base(configuration)
    {
    }

    public Task ContributeAsync(IInfoBuilder builder)
    {
        Contribute(builder, AppsettingsPrefix, false);
        return Task.CompletedTask;
    }
}
