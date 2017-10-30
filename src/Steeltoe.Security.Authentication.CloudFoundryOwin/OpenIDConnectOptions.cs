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

namespace Steeltoe.Security.Authentication.CloudFoundryOwin
{
    public class OpenIDConnectOptions : AuthenticationOptions
    {
        public OpenIDConnectOptions() : base(Constants.DefaultAuthenticationType)
        {
            Description.Caption = Constants.DefaultAuthenticationType;
            CallbackPath = new PathString("/signin-oidc");
            AuthenticationMode = AuthenticationMode.Passive;
        }

        public PathString CallbackPath { get; set; }

        public string AuthDomain { get; set; }

        public string ClientID { get; set; }

        public string ClientSecret { get; set; }

        public string AppHost { get; set; }

        public int AppPort { get; set; }

        public string SignInAsAuthenticationType { get; set; }

        public ISecureDataFormat<AuthenticationProperties> StateDataFormat { get; set; }
    }
}
