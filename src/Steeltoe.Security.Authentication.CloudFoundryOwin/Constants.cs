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

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin
{
    public static class Constants
    {
        public const string DefaultAuthenticationType = "PivotalSSO";

        public const string EndPointOAuthAuthorize = "oauth/authorize";
        public const string EndPointOAuthToken = "oauth/token";

        public const string ParamsClientID = "client_id";
        public const string ParamsClientSecret = "client_secret";
        public const string ParamsResponseType = "response_type";
        public const string ParamsScope = "scope";
        public const string ParamsRedirectUri = "redirect_uri";
        public const string ParamsGrantType = "grant_type";
        public const string ParamsTokenFormat = "token_format";
        public const string ParamsCode = "code";

        public const string ScopeOpenID = "openid";
        public const string GrantTypeAuthorizationCode = "authorization_code";
        public const string ResponseTypeIDToken = "id_token";
        public const string TokenFormatOpaque = "opaque";
    }
}
