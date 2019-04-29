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

using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryJwtOwinConfigurer
    {
        /// <summary>
        /// Apply service binding info to JWT options
        /// </summary>
        /// <param name="si">Info for bound SSO Service</param>
        /// <param name="options">Options to be updated</param>
        internal static void Configure(SsoServiceInfo si, CloudFoundryJwtBearerAuthenticationOptions options)
        {
            if (options == null)
            {
                return;
            }

            if (si != null)
            {
                options.JwtKeyUrl = si.AuthDomain + CloudFoundryDefaults.JwtTokenUri;
            }

            var backchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
            options.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(options.TokenValidationParameters, options.JwtKeyUrl, backchannelHttpHandler, options.ValidateCertificates);
        }
    }
}
