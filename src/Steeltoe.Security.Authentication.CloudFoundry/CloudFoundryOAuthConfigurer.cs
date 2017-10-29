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

using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common;
using System.Net.Http;


namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryOAuthConfigurer
    {
        internal static void Configure(SsoServiceInfo si, CloudFoundryOAuthOptions options)
        {
            if (options == null)
            {
                return;
            }
            if (si != null)
            {
                options.ClientId = si.ClientId;
                options.ClientSecret = si.ClientSecret;
                options.AuthorizationEndpoint = si.AuthDomain + CloudFoundryDefaults.AuthorizationUri;
                options.TokenEndpoint = si.AuthDomain + CloudFoundryDefaults.AccessTokenUri;
                options.UserInformationEndpoint = si.AuthDomain + CloudFoundryDefaults.UserInfoUri;
                options.TokenInfoUrl = si.AuthDomain + CloudFoundryDefaults.CheckTokenUri;
                
            }

            options.BackchannelHttpHandler = CloudFoundryHelper.GetBackChannelHandler(options.ValidateCertificates);
           
        }




    }
}
