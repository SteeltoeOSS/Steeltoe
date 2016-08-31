//
// Copyright 2015 the original author or authors.
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

using Microsoft.Extensions.Options;
using SteelToe.CloudFoundry.Connector.Services;


namespace SteelToe.CloudFoundry.Connector.OAuth
{
    public class OAuthConfigurer
    {

        internal IOptions<OAuthOptions> Configure(SsoServiceInfo si, OAuthConnectorOptions configuration)
        {
            OAuthOptions ssoOptions = new OAuthOptions();
            UpdateOptions(configuration, ssoOptions);
            UpdateOptions(si, ssoOptions);
            return new ConnectorIOptions<OAuthOptions>(ssoOptions);

        }

        internal void UpdateOptions(SsoServiceInfo si, OAuthOptions options)
        {
            if (si == null)
            {
                return;
            }

            options.ClientId = si.ClientId;
            options.ClientSecret = si.ClientSecret;
            options.AccessTokenUrl = si.AuthDomain + OAuthConnectorOptions.Default_AccessTokenUri;
            options.UserAuthorizationUrl = si.AuthDomain + OAuthConnectorOptions.Default_AuthorizationUri;
            options.TokenInfoUrl = si.AuthDomain + OAuthConnectorOptions.Default_CheckTokenUri;
            options.UserInfoUrl = si.AuthDomain + OAuthConnectorOptions.Default_UserInfoUri;
            options.JwtKeyUrl = si.AuthDomain + OAuthConnectorOptions.Default_JwtTokenKey;
        }

        internal void UpdateOptions(OAuthConnectorOptions config, OAuthOptions options)
        {
            if (config == null)
            {
                return;
            }
            options.ClientId = config.ClientId;
            options.ClientSecret = config.ClientSecret;
            options.AccessTokenUrl = config.OAuthServiceUrl + config.AccessTokenUri;
            options.UserAuthorizationUrl = config.OAuthServiceUrl + config.UserAuthorizationUri;
            options.TokenInfoUrl = config.OAuthServiceUrl + config.TokenInfoUri;
            options.UserInfoUrl = config.OAuthServiceUrl + config.UserInfoUri;
            options.JwtKeyUrl = config.OAuthServiceUrl + config.JwtKeyUri;
            if (config.Scope != null)
            {
                foreach(var scope in config.Scope)
                {
                    options.Scope.Add(scope);
                }
            }

            return;
        }
    }

}
