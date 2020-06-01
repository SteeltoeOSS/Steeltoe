// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
