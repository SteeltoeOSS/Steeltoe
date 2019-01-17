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

using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.ServiceModel.Web;
using System.Text;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class JwtAuthorizationManagerTest
    {
        private readonly CloudFoundryOptions _options = new CloudFoundryOptions() { AuthorizationUrl = "http://localhost" };
        private readonly string tokenKeysJsonString = @"{'keys':[{'kty':'RSA','e':'AQAB','use':'sig','kid':'key-1','alg':'RS256','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyyt7z23ctJP3UStbx0b/\nLbLWbQfEfOAHh09x7BnFB7vLw4cngzy685LrQyi12FKHDx+0Ux3B4o5hvfHLmgml\npXrWUjf0G3aE77FfMhulmIS9avlNjSUErO7Tgq4/XYiasfWeiIfjhs4cQvOwZeF3\nSBlj5VdHakLertr2DAIigmlziCOkb4is67dfGLZkc8UeKTJmueW56jlT9hyCRjBM\nGbV9LNiaZ6vp+jwk2ugW0pIjMbyfMxoIExiMQmYwT0TP/n8cd89eaKqmO2HXiYL9\nyqqTMMCV6I2lXNxXCEu/cii7kj9Il4aLowzWHJ0Z4XPJsTufV8uZShYxV+gBSekM\nLwIDAQAB\n-----END PUBLIC KEY-----','n':'AMsre89t3LST91ErW8dG_y2y1m0HxHzgB4dPcewZxQe7y8OHJ4M8uvOS60MotdhShw8ftFMdweKOYb3xy5oJpaV61lI39Bt2hO-xXzIbpZiEvWr5TY0lBKzu04KuP12ImrH1noiH44bOHELzsGXhd0gZY-VXR2pC3q7a9gwCIoJpc4gjpG-IrOu3Xxi2ZHPFHikyZrnlueo5U_YcgkYwTBm1fSzYmmer6fo8JNroFtKSIzG8nzMaCBMYjEJmME9Ez_5_HHfPXmiqpjth14mC_cqqkzDAleiNpVzcVwhLv3Iou5I_SJeGi6MM1hydGeFzybE7n1fLmUoWMVfoAUnpDC8'},{'kty':'RSA','e':'AQAB','use':'sig','kid':'key-2','alg':'RS256','value':'some super simple key that just happens to have enough characters','n':'someNvalue'}]}";

        [Fact]
        public void JwtAuthorizationManager_Requires_Options()
        {
            // arrange
            var manager = new JwtAuthorizationManager();

            // act
            var exception = Assert.Throws<WebFaultException<string>>(() => manager.GetPrincipalFromRequestHeaders(null));

            // assert
            Assert.Equal("SSO Configuration is missing", exception.Detail);
        }

        [Fact]
        public void JwtAuthorizationManager_Requires_AuthorizationHeader()
        {
            // arrange
            var manager = new JwtAuthorizationManager(_options);
            var headers = new WebHeaderCollection();

            // act
            var exception = Assert.Throws<WebFaultException<string>>(() => manager.GetPrincipalFromRequestHeaders(headers));

            // assert
            Assert.Equal("No Authorization header", exception.Detail);
        }

        [Fact]
        public void JwtAuthorizationManager_Requires_BearerTokenFormat()
        {
            // arrange
            var manager = new JwtAuthorizationManager(_options);
            var headers = new WebHeaderCollection
            {
                { HttpRequestHeader.Authorization, "bear" }
            };

            // act
            var exception = Assert.Throws<WebFaultException<string>>(() => manager.GetPrincipalFromRequestHeaders(headers));

            // assert
            Assert.Equal("Wrong Token Format", exception.Detail);
        }

        [Fact]
        public void JwtAuthorizationManager_Requires_An_Actual_Token()
        {
            // arrange
            var manager = new JwtAuthorizationManager(_options);
            var headers = new WebHeaderCollection
            {
                { HttpRequestHeader.Authorization, "Bearer " }
            };

            // act
            var exception = Assert.Throws<WebFaultException<string>>(() => manager.GetPrincipalFromRequestHeaders(headers));

            // assert
            Assert.Equal("No Token", exception.Detail);
        }

        [Fact]
        public void JwtAuthorizationManager_ThrowsInvalidToken()
        {
            // arrange
            _options.TokenKeyResolver = null;
            _options.TokenKeyResolver = new CloudFoundry.CloudFoundryTokenKeyResolver(_options.AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, GetMockHttpMessageHandler(), false);
            _options.TokenValidator.Options = _options;
            _options.TokenValidationParameters = null;
            _options.TokenValidationParameters = _options.GetTokenValidationParameters();
            var manager = new JwtAuthorizationManager(_options);
            var headers = new WebHeaderCollection
            {
                { HttpRequestHeader.Authorization, "Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJqdGkiOiI3YTMzYzVhNjhjY2I0YjRiYmQ5N2I4MTRlZWExMTc3MiIsInN1YiI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsInNjb3BlIjpbInRlc3Rncm91cCIsIm9wZW5pZCJdLCJjbGllbnRfaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJjaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJhenAiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJncmFudF90eXBlIjoiYXV0aG9yaXphdGlvbl9jb2RlIiwidXNlcl9pZCI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsIm9yaWdpbiI6InVhYSIsInVzZXJfbmFtZSI6ImRhdmUiLCJlbWFpbCI6ImRhdmVAdGVzdGNsb3VkLmNvbSIsImF1dGhfdGltZSI6MTU0NjY0MjkzMywicmV2X3NpZyI6ImE1ZWY2ODg5IiwiaWF0IjoxNTQ2NjQyOTM1LCJleHAiOjE1NDY2ODYxMzUsImlzcyI6Imh0dHBzOi8vc3RlZWx0b2UudWFhLmNmLmJlZXQuc3ByaW5nYXBwcy5pby9vYXV0aC90b2tlbiIsInppZCI6IjNhM2VhZGFkLTViMmYtNDUzMC1hZjk1LWE2OWJjMGFmZDE1YiIsImF1ZCI6WyJvcGVuaWQiLCJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiXX0.tGTXZzuuUSObTwdPHSx-zvnld20DH5hlOZlYp5DhjwkMIsZB0uIvVwbVDkPp7H_AmmeJoo6vqa5hbbgfgnYpTrKlCGOypnHoa3yRIKrwcDmLLujaMz6ApZeaJ7sJN-0N1UnPZ9iGcqvt9hNb_198zRnMXGH72oI0e2iGUBV1olCFVdZTnMGT7sUieDFKy7n0ghZYq_gUI8rfvTwiC3lfxv0nDXz4oE9Z-UKhK6q1zkAtQrz61FQ_CHONejz1JnuxQFKMMvm8JLcRkn6OL-EcSi1hkmFw0efO1OqccQacxphlafyHloVPQ3IOtzLjCf8sJ5NgTdCTC3iddT_sYovdrg" }
            };

            // act
            var exception = Assert.Throws<WebFaultException<string>>(() => manager.GetPrincipalFromRequestHeaders(headers));

            // assert
            Assert.StartsWith("IDX10223: Lifetime validation failed", exception.Detail);
        }

        [Fact(Skip = "fails at signature validation")]
        public void JwtAuthorizationManager_ReturnsPrincipalFromToken()
        {
            // arrange
            _options.TokenKeyResolver = null;
            _options.TokenKeyResolver = new CloudFoundry.CloudFoundryTokenKeyResolver(_options.AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri, GetMockHttpMessageHandler(), false);
            _options.TokenValidator.Options = _options;
            _options.TokenValidationParameters = null;
            _options.TokenValidationParameters = _options.GetTokenValidationParameters();
            var manager = new JwtAuthorizationManager(_options);
            var headers = new WebHeaderCollection
            {
                { HttpRequestHeader.Authorization, $"Bearer {CreateJwt()}" }
            };

            // act
            var principal = manager.GetPrincipalFromRequestHeaders(headers);

            // assert
            Assert.NotNull(principal);
            Assert.Equal("dave", principal.Identity.Name);
        }

        private HttpMessageHandler GetMockHttpMessageHandler()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp
                .When(HttpMethod.Get, _options.AuthorizationUrl + CloudFoundryDefaults.JwtTokenUri)
                .Respond("application/json", tokenKeysJsonString); // Respond with JSON

            return mockHttp;
        }

        private string CreateJwt()
        {
            var signingKey = "some super simple key that just happens to have enough characters";
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var header = new JwtHeader(new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature))
            {
                { "kid", "key-2" }
            };
            var payload = new JwtPayload("uaa", "tests", new List<Claim> { new Claim("scope", "openid") }, null, DateTime.Now.AddMinutes(5));
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(secToken);
            return token;

            // {
            //                {  "jti": "7a33c5a68ccb4b4bbd97b814eea11772" },
            //                {  "sub": "95bbb346-b68c-4f15-b341-70d60f9f46ba"},
            //                  "scope": [ "testgroup", "openid" ],
            //  "client_id": "c920b4f5-487c-4dd0-a63d-4d40c1331986",
            //  "cid": "c920b4f5-487c-4dd0-a63d-4d40c1331986",
            //  "azp": "c920b4f5-487c-4dd0-a63d-4d40c1331986",
            //  "grant_type": "authorization_code",
            //  "user_id": "95bbb346-b68c-4f15-b341-70d60f9f46ba",
            //  "origin": "uaa",
            //  "user_name": "dave",
            //  "email": "dave@testcloud.com",
            //  "auth_time": 1546642933,
            //  "rev_sig": "a5ef6889",
            //  "iat": 1546642935,
            //  "exp": 1546686135,
            //  "iss": "https://steeltoe.uaa.cf.beet.springapps.io/oauth/token",
            //  "zid": "3a3eadad-5b2f-4530-af95-a69bc0afd15b",
            //  "aud": [
            //    "openid",
            //    "c920b4f5-487c-4dd0-a63d-4d40c1331986"
            //  ]
            //    }
            // }
        }
    }
}
