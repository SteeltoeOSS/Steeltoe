// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Extensions.Configuration.CloudFoundry;

public static class CloudFoundryConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder configurationBuilder)
    {
        return configurationBuilder.AddCloudFoundry(null);
    }

    public static IConfigurationBuilder AddCloudFoundry(this IConfigurationBuilder configurationBuilder, ICloudFoundrySettingsReader settingsReader)
    {
        if (configurationBuilder == null)
        {
            throw new ArgumentNullException(nameof(configurationBuilder));
        }

        return configurationBuilder.Add(new CloudFoundryConfigurationSource { SettingsReader = settingsReader });
    }
}
