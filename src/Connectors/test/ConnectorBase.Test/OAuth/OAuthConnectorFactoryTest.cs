// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test
{
    public class OAuthConnectorFactoryTest
    {
        [Fact]
        public void Create_ReturnsOAuthOptions()
        {
            var si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");
            var config = new OAuthConnectorOptions();

            var factory = new OAuthConnectorFactory(si, config);
            var result = factory.Create(null);

            Assert.NotNull(result);
            var opts = result.Value;
            Assert.NotNull(opts);

            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AccessTokenUri, opts.AccessTokenUrl);
            Assert.Equal("myClientId", opts.ClientId);
            Assert.Equal("myClientSecret", opts.ClientSecret);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_JwtTokenKey, opts.JwtKeyUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_CheckTokenUri, opts.TokenInfoUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_AuthorizationUri, opts.UserAuthorizationUrl);
            Assert.Equal("https://foo.bar" + OAuthConnectorDefaults.Default_UserInfoUri, opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }
    }
}
