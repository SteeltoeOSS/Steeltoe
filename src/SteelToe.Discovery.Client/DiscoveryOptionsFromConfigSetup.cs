//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.OptionsModel;
using System;
using System.Linq;

namespace SteelToe.Discovery.Client
{
    public class DiscoveryOptionsFromConfigSetup : ConfigureOptions<DiscoveryOptions>
    {
        public const string EUREKA_PREFIX = "eureka";
        public const string EUREKA_CLIENT_CONFIGURATION_PREFIX = "eureka:client";
        public const string EUREKA_INSTANCE_CONFIGURATION_PREFIX = "eureka:instance";

        public DiscoveryOptionsFromConfigSetup(IConfiguration config)
            : base(options => ConfigureDiscovery(config, options))
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
        }

        internal static void ConfigureDiscovery(IConfiguration config, DiscoveryOptions options)
        {
            var clientConfigsection = config.GetSection(EUREKA_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            if (childCount > 0)
            {
                ConfigureDiscovery(config, options, new EurekaClientOptions(), new EurekaInstanceOptions());
            }
            else
            {
                options.ClientType = DiscoveryClientType.UNKNOWN;
            }
        }

        internal static void ConfigureDiscovery(IConfiguration config, DiscoveryOptions options, EurekaClientOptions clientOptions, EurekaInstanceOptions instOptions)
        {
       
            ConfigurationBinder.Bind(config, clientOptions);
            ConfigurationBinder.Bind(config, instOptions);

            options.ClientType = DiscoveryClientType.EUREKA;
            options.ClientOptions = clientOptions;
            options.RegistrationOptions = instOptions;

        }

    }
}
