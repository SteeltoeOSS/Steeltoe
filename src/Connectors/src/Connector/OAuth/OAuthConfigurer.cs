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

    internal void UpdateOptions(SsoServiceInfo si, OAuthServiceOptions serviceOptions)
    {
        if (si == null)
        {
            return;
        }

        serviceOptions.ClientId = si.ClientId;
        serviceOptions.ClientSecret = si.ClientSecret;
        serviceOptions.AccessTokenUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultAccessTokenUri;
        serviceOptions.UserAuthorizationUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultAuthorizationUri;
        serviceOptions.TokenInfoUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultCheckTokenUri;
        serviceOptions.UserInfoUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultUserInfoUri;
        serviceOptions.JwtKeyUrl = si.AuthDomain + OAuthConnectorDefaults.DefaultJwtTokenKey;
    }

    internal void UpdateOptions(OAuthConnectorOptions connectorOptions, OAuthServiceOptions serviceOptions)
    {
        if (connectorOptions == null)
        {
            return;
        }

        serviceOptions.ClientId = connectorOptions.ClientId;
        serviceOptions.ClientSecret = connectorOptions.ClientSecret;
        serviceOptions.AccessTokenUrl = connectorOptions.OAuthServiceUrl + connectorOptions.AccessTokenUri;
        serviceOptions.UserAuthorizationUrl = connectorOptions.OAuthServiceUrl + connectorOptions.UserAuthorizationUri;
        serviceOptions.TokenInfoUrl = connectorOptions.OAuthServiceUrl + connectorOptions.TokenInfoUri;
        serviceOptions.UserInfoUrl = connectorOptions.OAuthServiceUrl + connectorOptions.UserInfoUri;
        serviceOptions.JwtKeyUrl = connectorOptions.OAuthServiceUrl + connectorOptions.JwtKeyUri;
        serviceOptions.ValidateCertificates = connectorOptions.ValidateCertificates;

        if (connectorOptions.Scope != null)
        {
            foreach (string scope in connectorOptions.Scope)
            {
                serviceOptions.Scope.Add(scope);
            }
        }
    }
}
