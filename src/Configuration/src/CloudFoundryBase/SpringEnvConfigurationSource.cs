// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.CloudFoundry
{
    public class SpringEnvConfigurationSource : IConfigurationSource
    {
        public ICloudFoundrySettingsReader SettingsReader { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SpringEnvConfigurationProvider(SettingsReader ?? new CloudFoundryEnvironmentSettingsReader());
        }
    }
}
