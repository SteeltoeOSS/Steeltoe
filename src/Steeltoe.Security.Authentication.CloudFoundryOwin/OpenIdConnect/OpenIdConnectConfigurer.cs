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

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public static class OpenIdConnectConfigurer
    {
        /// <summary>
        /// Apply service binding info to an <see cref="OpenIdConnectOptions"/> instance
        /// </summary>
        /// <param name="si">Service binding information</param>
        /// <param name="options">OpenID options to be updated</param>
        internal static void Configure(SsoServiceInfo si, OpenIdConnectOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (si == null)
            {
                return;
            }

            options.AuthDomain = si.AuthDomain;
            options.ClientId = si.ClientId;
            options.ClientSecret = si.ClientSecret;
        }
    }
}
