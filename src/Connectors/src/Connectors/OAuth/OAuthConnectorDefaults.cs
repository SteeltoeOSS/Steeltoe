// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.OAuth;

public static class OAuthConnectorDefaults
{
    public const string DefaultAuthorizationUri = "/oauth/authorize";
    public const string DefaultAccessTokenUri = "/oauth/token";
    public const string DefaultUserInfoUri = "/userinfo";
    public const string DefaultCheckTokenUri = "/check_token";
    public const string DefaultJwtTokenKey = "/token_keys";
    public const string DefaultOAuthServiceUrl = "Default_OAuthServiceUrl";
    public const string DefaultClientId = "Default_ClientId";
    public const string DefaultClientSecret = "Default_ClientSecret";
    public const bool DefaultValidateCertificates = true;
}
