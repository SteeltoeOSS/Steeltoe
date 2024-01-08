// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.CloudFoundry;

internal sealed class CloudFoundryConfigurationSource : IConfigurationSource
{
    private readonly ICloudFoundrySettingsReader _settingsReader;

    public CloudFoundryConfigurationSource()
        : this(null)
    {
    }

    public CloudFoundryConfigurationSource(ICloudFoundrySettingsReader? settingsReader)
    {
        _settingsReader = settingsReader ?? new CloudFoundryEnvironmentSettingsReader();
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new CloudFoundryConfigurationProvider(_settingsReader);
    }
}
