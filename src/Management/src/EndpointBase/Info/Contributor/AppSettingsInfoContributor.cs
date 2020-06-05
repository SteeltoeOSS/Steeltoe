// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public class AppSettingsInfoContributor : AbstractConfigurationContributor, IInfoContributor
    {
        private const string APPSETTINGS_PREFIX = "info";

        public AppSettingsInfoContributor(IConfiguration config)
            : base(config)
        {
        }

        public void Contribute(IInfoBuilder builder)
        {
            Contribute(builder, APPSETTINGS_PREFIX, false);
        }
    }
}
