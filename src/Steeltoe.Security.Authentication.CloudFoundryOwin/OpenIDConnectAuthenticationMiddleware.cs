//
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
//

using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.DataHandler;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace Steeltoe.Security.Authentication.CloudFoundryOwin
{
    public class OpenIDConnectAuthenticationMiddleware : AuthenticationMiddleware<OpenIDConnectOptions>
    {
        public OpenIDConnectAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, OpenIDConnectOptions options)
               : base(next, options)
        {
            if (string.IsNullOrEmpty(Options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            if (options.StateDataFormat == null)
            {
                var dataProtector = app.CreateDataProtector(typeof(OpenIDConnectAuthenticationMiddleware).FullName,
                    options.AuthenticationType);

                options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }
        }

        protected override AuthenticationHandler<OpenIDConnectOptions> CreateHandler()
        {
            return new OpenIDConnectAuthenticationHandler();
        }
    }
}

