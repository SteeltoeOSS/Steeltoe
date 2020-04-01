// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Steeltoe.Security.Authentication.Mtls;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Enable certificate rotation and forwarding
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/></param>
        public static IApplicationBuilder UseCloudFoundryContainerIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app
                    .UseCertificateRotation()
                    .UseCertificateForwarding();
        }

        /// <summary>
        /// Enable identity certificate rotation, certificate and header forwarding, authentication and authorization
        /// Default ForwardedHeadersOptions only includes <see cref="ForwardedHeaders.XForwardedProto"/>
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/></param>
        /// <param name="forwardedHeaders">Custom header forwarding policy</param>
        public static IApplicationBuilder UseCloudFoundryCertificateAuth(this IApplicationBuilder app, ForwardedHeadersOptions forwardedHeaders = null)
        {
            app.UseForwardedHeaders(forwardedHeaders ?? new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });
            app.UseCloudFoundryContainerIdentity();
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}