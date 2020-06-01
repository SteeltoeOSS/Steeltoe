// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
