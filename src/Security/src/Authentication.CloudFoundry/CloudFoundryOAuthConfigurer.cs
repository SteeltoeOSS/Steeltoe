// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;

namespace Steeltoe.Security.Authentication.CloudFoundry;

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
