// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Infrastructure;
using Owin;
using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public class OpenIdConnectAuthenticationMiddleware : AuthenticationMiddleware<OpenIdConnectOptions>
    {
        public OpenIdConnectAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, OpenIdConnectOptions options)
               : base(next, options)
        {
            if (string.IsNullOrEmpty(Options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }

            if (options.StateDataFormat == null)
            {
                var dataProtector = app.CreateDataProtector(
                    typeof(OpenIdConnectAuthenticationMiddleware).FullName,
                    options.AuthenticationType);

                options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }
        }

        protected override AuthenticationHandler<OpenIdConnectOptions> CreateHandler()
        {
            return new OpenIdConnectAuthenticationHandler(Options.LoggerFactory?.CreateLogger("OpenIdConnectAuthenticationHandler"));
        }
    }
}
