
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

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
            base.Contribute(builder, APPSETTINGS_PREFIX, false);
        }

    }
}
