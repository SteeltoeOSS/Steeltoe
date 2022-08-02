// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.OAuth;

public class OAuthConfigurer
{
    internal IOptions<OAuthServiceOptions> Configure(SsoServiceInfo si, OAuthConnectorOptions configuration)
    {
        var ssoOptions = new OAuthServiceOptions();
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
        options.AccessTokenUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultAccessTokenUri;
        options.UserAuthorizationUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultAuthorizationUri;
        options.TokenInfoUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultCheckTokenUri;
        options.UserInfoUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultUserInfoUri;
        options.JwtKeyUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultJwtTokenKey;
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
            foreach (string scope in config.Scope)
            {
                options.Scope.Add(scope);
            }
        }
    }
}
