// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryDefaults
    {
        public const string SECURITY_CLIENT_SECTION_PREFIX = "security:oauth2:client";
        public const string SECURITY_RESOURCE_SECTION_PREFIX = "security:oauth2:resource";

        public const string AuthenticationScheme = "CloudFoundry";
        public const string DisplayName = "CloudFoundry";

        public const string AuthorizationUri = "/oauth/authorize";
        public const string AccessTokenUri = "/oauth/token";
        public const string UserInfoUri = "/userinfo";
        public const string CheckTokenUri = "/check_token";
        public const string JwtTokenUri = "/token_keys";

        public const string OAuthServiceUrl = "Default_OAuthServiceUrl";
        public const string ClientId = "Default_ClientId";
        public const string ClientSecret = "Default_ClientSecret";
        public const string CallbackPath = "/signin-cloudfoundry";

        public const bool ValidateCertificates = true;

        public const string ParamsClientId = "client_id";
        public const string ParamsClientSecret = "client_secret";
        public const string ParamsResponseType = "response_type";
        public const string ParamsScope = "scope";
        public const string ParamsRedirectUri = "redirect_uri";
        public const string ParamsGrantType = "grant_type";
        public const string ParamsTokenFormat = "token_format";
        public const string ParamsCode = "code";

        public const string SameOrganizationAuthorizationPolicy = "sameorg";
        public const string SameSpaceAuthorizationPolicy = "samespace";
    }
}
