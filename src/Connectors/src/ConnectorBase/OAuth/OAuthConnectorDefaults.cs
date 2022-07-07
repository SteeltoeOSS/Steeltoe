// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.OAuth;

public static class OAuthConnectorDefaults
{
    public const string Default_AuthorizationUri = "/oauth/authorize";
    public const string Default_AccessTokenUri = "/oauth/token";
    public const string Default_UserInfoUri = "/userinfo";
    public const string Default_CheckTokenUri = "/check_token";
    public const string Default_JwtTokenKey = "/token_keys";
    public const string Default_OAuthServiceUrl = "Default_OAuthServiceUrl";
    public const string Default_ClientId = "Default_ClientId";
    public const string Default_ClientSecret = "Default_ClientSecret";
    public const bool Default_ValidateCertificates = true;
}
