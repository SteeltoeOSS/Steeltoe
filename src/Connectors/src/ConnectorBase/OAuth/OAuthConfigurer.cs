// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector.Services;

namespace Steeltoe.CloudFoundry.Connector.OAuth
{
    public class OAuthConfigurer
    {
        internal IOptions<OAuthServiceOptions> Configure(SsoServiceInfo si, OAuthConnectorOptions configuration)
        {
            OAuthServiceOptions ssoOptions = new OAuthServiceOptions();
            UpdateOptions(configuration, ssoOptions);
            UpdateOptions(si, ssoOptions);
            return new ConnectorIOptions<OAuthServiceOptions>(ssoOptions);
        }

        internal void UpdateOptions(SsoServiceInfo si, OAuthServiceOptions options)
        {
            if (si == null)
            {
                return;
            }

            options.ClientId = si.ClientId;
            options.ClientSecret = si.ClientSecret;
            options.AccessTokenUrl = si.AuthDomain + OAuthConnectorDefaults.Default_AccessTokenUri;
            options.UserAuthorizationUrl = si.AuthDomain + OAuthConnectorDefaults.Default_AuthorizationUri;
            options.TokenInfoUrl = si.AuthDomain + OAuthConnectorDefaults.Default_CheckTokenUri;
            options.UserInfoUrl = si.AuthDomain + OAuthConnectorDefaults.Default_UserInfoUri;
            options.JwtKeyUrl = si.AuthDomain + OAuthConnectorDefaults.Default_JwtTokenKey;
        }

        internal void UpdateOptions(OAuthConnectorOptions config, OAuthServiceOptions options)
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
            options.ValidateCertificates = config.ValidateCertificates;
            if (config.Scope != null)
            {
                foreach (var scope in config.Scope)
                {
                    options.Scope.Add(scope);
                }
            }
        }
    }
}
