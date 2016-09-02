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

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using SteelToe.CloudFoundry.Connector.OAuth;

namespace SteelToe.Security.Authentication.CloudFoundry
{

    public static class CloudFoundryAppBuilderExtensions
    {
        public static IApplicationBuilder UseCloudFoundryAuthentication(this IApplicationBuilder builder )
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var iopts = builder.ApplicationServices.GetService(typeof(IOptions<OAuthServiceOptions>)) as IOptions<OAuthServiceOptions>;
            var signonOpts = iopts?.Value;

            CloudFoundryOptions cloudOpts = null;
            if (signonOpts != null)
            {
                cloudOpts = new CloudFoundryOptions(signonOpts);
            } else
            {
                cloudOpts = new CloudFoundryOptions();
            }
   

            return builder.UseMiddleware<CloudFoundryMiddleware>(Options.Create(cloudOpts));
        }

        public static IApplicationBuilder UseCloudFoundryAuthentication(this IApplicationBuilder builder, CloudFoundryOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseMiddleware<CloudFoundryMiddleware>(Options.Create(options));
        }

    }
}
