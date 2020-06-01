// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public static class Constants
    {
        public const string DefaultAuthenticationType = "PivotalSSO";

        [Obsolete("Use CloudFoundryDefaults.AuthorizationUri instead")]
        public const string EndPointOAuthAuthorize = CloudFoundryDefaults.AuthorizationUri;
        [Obsolete("Use CloudFoundryDefaults.AccessTokenUri instead")]
        public const string EndPointOAuthToken = CloudFoundryDefaults.AccessTokenUri;
        [Obsolete("Use CloudFoundryDefaults.ParamsClientID instead")]
        public const string ParamsClientID = CloudFoundryDefaults.ParamsClientId;
        [Obsolete("Use CloudFoundryDefaults.ParamsClientSecret instead")]
        public const string ParamsClientSecret = CloudFoundryDefaults.ParamsClientSecret;
        [Obsolete("Use CloudFoundryDefaults.ParamsResponseType instead")]
        public const string ParamsResponseType = CloudFoundryDefaults.ParamsResponseType;
        [Obsolete("Use CloudFoundryDefaults.ParamsScope instead")]
        public const string ParamsScope = CloudFoundryDefaults.ParamsScope;
        [Obsolete("Use CloudFoundryDefaults.ParamsRedirectUri instead")]
        public const string ParamsRedirectUri = CloudFoundryDefaults.ParamsRedirectUri;
        [Obsolete("Use CloudFoundryDefaults.ParamsGrantType instead")]
        public const string ParamsGrantType = CloudFoundryDefaults.ParamsGrantType;
        [Obsolete("Use CloudFoundryDefaults.ParamsTokenFormat instead")]
        public const string ParamsTokenFormat = CloudFoundryDefaults.ParamsTokenFormat;
        [Obsolete("Use CloudFoundryDefaults.ParamsCode instead")]
        public const string ParamsCode = CloudFoundryDefaults.ParamsCode;

        public const string ScopeOpenID = "openid";
        public const string GrantTypeAuthorizationCode = "authorization_code";
        public const string ResponseTypeIDToken = "id_token";
        public const string TokenFormatOpaque = "opaque";
    }
}
