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
