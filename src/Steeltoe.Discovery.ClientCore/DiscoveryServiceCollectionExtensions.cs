//
// Copyright 2017 the original author or authors.
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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka;
using System;
using System.Linq;
using System.Threading;

namespace Steeltoe.Discovery.Client
{

    public static class DiscoveryServiceCollectionExtensions
    {
        public const string EUREKA_PREFIX = "eureka";

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, DiscoveryOptions discoveryOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (discoveryOptions == null)
            {
                throw new ArgumentNullException(nameof(discoveryOptions));
            }

 
            if (discoveryOptions.ClientType == DiscoveryClientType.EUREKA)
            {
                EurekaClientOptions clientOptions = discoveryOptions.ClientOptions as EurekaClientOptions;
                if (clientOptions == null)
                {
                    throw new ArgumentException("Missing Client Options");
                }

                services.AddSingleton<IOptionsMonitor<EurekaClientOptions>>(new OptionsMonitorWrapper<EurekaClientOptions>(clientOptions));

                var regOptions = discoveryOptions.RegistrationOptions as EurekaInstanceOptions;
                if  (regOptions == null)
                {
                    clientOptions.ShouldRegisterWithEureka = false;
                    regOptions = new EurekaInstanceOptions();
                }

                services.AddSingleton<IOptionsMonitor<EurekaInstanceOptions>>(new OptionsMonitorWrapper<EurekaInstanceOptions>(regOptions));

                AddEurekaServices(services);
            }
            else
            {
                throw new ArgumentException("Client type UNKNOWN");
            }

            return services;
        }

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, Action<DiscoveryOptions> setupOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupOptions == null)
            {
                throw new ArgumentNullException(nameof(setupOptions));
            }

            var options = new DiscoveryOptions();
            setupOptions(options);

            return services.AddDiscoveryClient(options);

        }


        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AddDiscoveryServices(services, config);

            return services;

        }
        private static void AddDiscoveryServices(IServiceCollection services, IConfiguration config)
        {
            var clientConfigsection = config.GetSection(EUREKA_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            if (childCount > 0)
            {
                var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
                services.Configure<EurekaClientOptions>(clientSection);

                var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
                services.Configure<EurekaInstanceOptions>(instSection);

                AddEurekaServices(services);
            }
            else
            {
                throw new ArgumentException("Discovery client type UNKNOWN, check configuration");
            }

        }

        private static void AddEurekaServices(IServiceCollection services)
        {
            services.AddSingleton<EurekaApplicationInfoManager>();
            services.AddSingleton<EurekaDiscoveryManager>();

            services.AddSingleton<EurekaDiscoveryClient>();
            services.AddSingleton<IDiscoveryLifecycle, ApplicationLifecycle>();
            services.AddSingleton<IDiscoveryClient>((p) => p.GetService<EurekaDiscoveryClient>());
        }

        public class ApplicationLifecycle : IDiscoveryLifecycle
        {

            public ApplicationLifecycle(IApplicationLifetime lifeCycle)
            {
                ApplicationStopping = lifeCycle.ApplicationStopping;
            }

            public CancellationToken ApplicationStopping { get; set; }
       
        }

        public class OptionsMonitorWrapper<T> : IOptionsMonitor<T>
        {
            private T _option;
            public OptionsMonitorWrapper(T option)
            {
                _option = option;
            }
            public T CurrentValue => _option;

            public T Get(string name)
            {
                return _option;
            }

            public IDisposable OnChange(Action<T, string> listener)
            {
                return null;
            }
        }
    }
}
