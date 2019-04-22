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
using Microsoft.Extensions.DependencyInjection;
using System;
using Steeltoe.CloudFoundry.Connector.OAuth;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryServiceCollectionExtensions
    {
        public static IServiceCollection AddCloudFoundryAuthentication(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }


            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CloudFoundryOptions.AUTHENTICATION_SCHEME);
            services.AddOAuthServiceOptions(config);
            return services;
        }

        public static IServiceCollection AddCloudFoundryJwtAuthentication(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }


            services.AddAuthentication();
            services.AddOAuthServiceOptions(config);
            return services;
        }

    }
}
