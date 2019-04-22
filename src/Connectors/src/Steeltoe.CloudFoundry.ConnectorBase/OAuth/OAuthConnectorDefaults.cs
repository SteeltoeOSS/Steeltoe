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

namespace Steeltoe.CloudFoundry.Connector.OAuth
{
    public class OAuthConnectorDefaults
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
}
