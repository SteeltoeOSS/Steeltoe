//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using Xunit;

namespace SteelToe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryTokenValidatorTest
    {
        [Fact]
        public void ValidateToken_ValidatesSignatureCorrectly()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            string keyset = "{ 'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            var keys = JsonWebKeySet.Create(keyset);
            var webKey = keys.Keys[0];


            var parameters = new TokenValidationParameters();
            CloudFoundryOptions options = new CloudFoundryOptions();
            options.TokenKeyResolver = new CloudFoundryTokenKeyResolver(options);
            options.TokenValidator = new CloudFoundryTokenValidator(options);
            options.TokenValidationParameters = parameters;
            options.TokenKeyResolver.FixupKey(webKey);
            options.TokenKeyResolver.Resolved["legacy-token-key"] = webKey;

            parameters.ValidateAudience = false;
            parameters.ValidateIssuer = false;
            parameters.ValidateLifetime = false;


            parameters.IssuerSigningKeyResolver = options.TokenKeyResolver.ResolveSigningKey;

            var result = options.TokenValidator.ValidateToken(token);
            Assert.True(result);
        }

        [Fact]
        public void ValidateToken_FailsOnLifetime()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            string keyset = "{ 'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            var keys = JsonWebKeySet.Create(keyset);
            var webKey = keys.Keys[0];


            var parameters = new TokenValidationParameters();
            CloudFoundryOptions options = new CloudFoundryOptions();
            options.TokenKeyResolver = new CloudFoundryTokenKeyResolver(options);
            options.TokenValidator = new CloudFoundryTokenValidator(options);
            options.TokenValidationParameters = parameters;
            options.TokenKeyResolver.FixupKey(webKey);
            options.TokenKeyResolver.Resolved["legacy-token-key"] = webKey;

            parameters.ValidateAudience = false;
            parameters.ValidateIssuer = false;
            parameters.ValidateLifetime = true;


            parameters.IssuerSigningKeyResolver = options.TokenKeyResolver.ResolveSigningKey;

            var result = options.TokenValidator.ValidateToken(token);
            Assert.False(result);
        }

        [Fact]
        public void ValidateAudience_ValidatesCorrectly()
        {
            CloudFoundryOptions options = new CloudFoundryOptions()
            {
                ClientId = "foobar"
            };

            var validator = new CloudFoundryTokenValidator(options);
            List<string> good = new List<string>() { "foobar", "barfoo" };
            List<string> bad = new List<string>() { "1234", "barfoo" };
            Assert.True(validator.ValidateAudience(good, null, null));
            Assert.False(validator.ValidateAudience(bad, null, null));
        }

        [Fact]
        public void ValidateIssuer_ValidatesCorrectly()
        {
            CloudFoundryOptions options = new CloudFoundryOptions();

            var validator = new CloudFoundryTokenValidator(options);

            Assert.NotNull(validator.ValidateIssuer("https://uaa.system.testcloud.com/", null, null));
            Assert.Null(validator.ValidateIssuer("https://foobar.system.testcloud.com/", null, null));
        }

        [Fact]
        public void GetAccessToken_FindsToken()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            Dictionary<string, string> items = new Dictionary<string, string>() { { CloudFoundryTokenValidator.ACCESS_TOKEN_KEY, token } };
            CloudFoundryOptions options = new CloudFoundryOptions();
            var validator = new CloudFoundryTokenValidator(options);
            var result = validator.GetAccessToken(items);
            Assert.NotNull(result);
        }
    }
}
