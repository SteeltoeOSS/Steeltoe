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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;

namespace Steeltoe.Management.Endpoint.Security
{
    public static class SecurityApplicationBuilderExtensions
    {
        /// <summary>
        /// Add Security Middleware for protecting sensitive endpoints
        /// </summary>
        /// <param name="builder">Your application builder</param>
        /// <param name="config">Configuration</param>
        /// <param name="loggerFactory">Logger Factory</param>
        public static void UseEndpointSecurity(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            var logger = loggerFactory?.CreateLogger<SecurityMiddleware>();
            builder.Use<SecurityMiddleware>(new CloudFoundryOptions(config), logger);
        }
    }
}
