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

using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class CloudFoundryTokenKeyResolverTest
    {
        private readonly string tokenKeysJsonString = @"{'keys':[{'kty':'RSA','e':'AQAB','use':'sig','kid':'key-1','alg':'RS256','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyyt7z23ctJP3UStbx0b/\nLbLWbQfEfOAHh09x7BnFB7vLw4cngzy685LrQyi12FKHDx+0Ux3B4o5hvfHLmgml\npXrWUjf0G3aE77FfMhulmIS9avlNjSUErO7Tgq4/XYiasfWeiIfjhs4cQvOwZeF3\nSBlj5VdHakLertr2DAIigmlziCOkb4is67dfGLZkc8UeKTJmueW56jlT9hyCRjBM\nGbV9LNiaZ6vp+jwk2ugW0pIjMbyfMxoIExiMQmYwT0TP/n8cd89eaKqmO2HXiYL9\nyqqTMMCV6I2lXNxXCEu/cii7kj9Il4aLowzWHJ0Z4XPJsTufV8uZShYxV+gBSekM\nLwIDAQAB\n-----END PUBLIC KEY-----','n':'AMsre89t3LST91ErW8dG_y2y1m0HxHzgB4dPcewZxQe7y8OHJ4M8uvOS60MotdhShw8ftFMdweKOYb3xy5oJpaV61lI39Bt2hO-xXzIbpZiEvWr5TY0lBKzu04KuP12ImrH1noiH44bOHELzsGXhd0gZY-VXR2pC3q7a9gwCIoJpc4gjpG-IrOu3Xxi2ZHPFHikyZrnlueo5U_YcgkYwTBm1fSzYmmer6fo8JNroFtKSIzG8nzMaCBMYjEJmME9Ez_5_HHfPXmiqpjth14mC_cqqkzDAleiNpVzcVwhLv3Iou5I_SJeGi6MM1hydGeFzybE7n1fLmUoWMVfoAUnpDC8'}]}";
        private readonly CloudFoundryOptions happyPathOptions = new CloudFoundryOptions() { AuthorizationUrl = "http://localhost" };
        private readonly CloudFoundryOptions networkFailOptions = new CloudFoundryOptions() { AuthorizationUrl = "http://localhost:81" };
        private readonly CloudFoundryOptions serviceUnavailableOptions = new CloudFoundryOptions() { AuthorizationUrl = "http://localhost:82" };

#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void TokenKeyResolver_Requires_Options()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new CloudFoundryTokenKeyResolver(null));
        }

        [Fact]
        public void ResolveSigningKey_ReturnsKey_FromServer()
        {
            // arrange a resolver that succeeds
            var tkr = new CloudFoundryTokenKeyResolver(happyPathOptions, GetMockHttpClient());

            // act
            var expected = tkr.ResolveSigningKey(string.Empty, null, "key-1", happyPathOptions.TokenValidationParameters);

            // assert
            Assert.NotNull(expected);
            var tokenKey = expected.First();
            Assert.IsType<JsonWebKey>(tokenKey);
            Assert.Equal("key-1", tokenKey.KeyId);
        }

        [Fact]
        public void ResolveSigningKey_ReturnsKeyPreviouslyResolved()
        {
            // arrange a resolver that has previously retrieved keys, but will fail going forward
            var tkr = new CloudFoundryTokenKeyResolver(happyPathOptions, GetMockHttpClient());
            tkr.ResolveSigningKey(string.Empty, null, "key-1", happyPathOptions.TokenValidationParameters);
            tkr.Options = networkFailOptions;
            Assert.Equal(networkFailOptions.AuthorizationUrl, tkr.Options.AuthorizationUrl);

            // act
            var expected = tkr.ResolveSigningKey(string.Empty, null, "key-1", happyPathOptions.TokenValidationParameters);

            // assert
            Assert.NotNull(expected);
            var tokenKey = expected.First();
            Assert.IsType<JsonWebKey>(tokenKey);
            Assert.Equal("key-1", tokenKey.KeyId);
        }

        [Fact]
        public void ResolveSigningKey_ReturnsNull_WhenNoKeyFound()
        {
            // arrange
            var tkr = new CloudFoundryTokenKeyResolver(serviceUnavailableOptions, GetMockHttpClient());

            // act
            var expected = tkr.ResolveSigningKey(string.Empty, null, "key-1", serviceUnavailableOptions.TokenValidationParameters);

            // assert
            Assert.Null(expected);
        }

        [Fact]
        public async void FetchKeySet_Throws_OnHttpClientException()
        {
            // arrange
            var tkr = new CloudFoundryTokenKeyResolver(networkFailOptions, GetMockHttpClient());

            // act
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => tkr.FetchKeySet());
        }

        [Fact]
        public async void FetchKeySet_ReturnsNull_OnFailure()
        {
            // arrange
            var tkr = new CloudFoundryTokenKeyResolver(serviceUnavailableOptions, GetMockHttpClient());

            // act
            var expected = await tkr.FetchKeySet();

            // assert
            Assert.Null(expected);
        }

        [Fact]
        public async void FetchKeySet_ReturnsKeySet_OnSuccess()
        {
            // arrange
            var tkr = new CloudFoundryTokenKeyResolver(happyPathOptions, GetMockHttpClient());

            // act
            var expected = await tkr.FetchKeySet();

            // assert
            Assert.Contains(expected.Keys, key => key.Kid == "key-1");
            var tokenKey = expected.Keys.First();
            Assert.Equal("RS256", tokenKey.Alg);
            Assert.Equal("sig", tokenKey.Use);
            Assert.Equal("AQAB", tokenKey.E);
        }

        [Fact]
        public void GetJsonWebKeySet_Parses_JsonString()
        {
            // arrange
            var tkr = new CloudFoundryTokenKeyResolver(happyPathOptions);

            // act
            var expected = tkr.GetJsonWebKeySet(tokenKeysJsonString);

            // assert
            Assert.Contains(expected.Keys, key => key.Kid == "key-1");
            var tokenKey = expected.Keys.First();
            Assert.Equal("RS256", tokenKey.Alg);
            Assert.Equal("sig", tokenKey.Use);
            Assert.Equal("AQAB", tokenKey.E);
        }

        private HttpClient GetMockHttpClient()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp
                .When("http://localhost:81" + CloudFoundryDefaults.JwtTokenUri)
                .Throw(new HttpRequestException()); // emulate a DNS failure
            mockHttp
                .When("http://localhost:82" + CloudFoundryDefaults.JwtTokenUri)
                .Respond(HttpStatusCode.ServiceUnavailable); // Respond unavailable
            mockHttp
                .When("http://localhost" + CloudFoundryDefaults.JwtTokenUri)
                .Respond("application/json", tokenKeysJsonString); // Respond with JSON

            return mockHttp.ToHttpClient();
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
