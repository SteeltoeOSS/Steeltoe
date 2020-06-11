// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    internal static class CloudFoundryOptionsConfigurer
    {
        /// <summary>
        /// Apply service binding info to an <see cref="CloudFoundryOptions"/> instance
        /// </summary>
        /// <param name="si">Service binding information</param>
        /// <param name="options">CloudFoundryOptions options to be updated</param>
        internal static void Configure(SsoServiceInfo si, CloudFoundryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var backchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
            options.TokenValidationParameters ??= options.GetTokenValidationParameters();
            options.TokenValidationParameters = CloudFoundryHelper.GetTokenValidationParameters(options.TokenValidationParameters, options.AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, backchannelHttpHandler, options.ValidateCertificates, options);

            if (si == null)
            {
                return;
            }

            options.AuthorizationUrl = si.AuthDomain;
            options.ClientId = si.ClientId;
            options.ClientSecret = si.ClientSecret;
        }
    }
}
