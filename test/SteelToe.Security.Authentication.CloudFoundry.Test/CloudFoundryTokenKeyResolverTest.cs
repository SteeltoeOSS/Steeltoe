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
using System;
using System.Linq;
using System.Net.Http;
using Xunit;

namespace SteelToe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryTokenKeyResolverTest
    {
        [Fact]
        public void Constructor_ThrowsIfOptionsNull()
        {
            // Arrange
            CloudFoundryOptions options = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new CloudFoundryTokenKeyResolver(options));
            Assert.Contains(nameof(options), ex.Message);

        }
        [Fact]
        public void ResolveSigningKey_FindsExistingKey()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            string keyset = "{ 'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            var keys = JsonWebKeySet.Create(keyset);
            var webKey = keys.Keys[0];

            var resolver = new CloudFoundryTokenKeyResolver(new CloudFoundryOptions());
            resolver.Resolved["legacy-token-key"] = webKey;

            var result = resolver.ResolveSigningKey(token, null, "legacy-token-key", null);
            Assert.True(result.First() == webKey);
        }

        [Fact]
        public void ResolveSigningKey_IssuesHttpRequest_ResolvesKey()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            string keyset = "{ 'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(keyset);
            handler.Response = response;
            CloudFoundryOptions options = new CloudFoundryOptions()
            {
                BackchannelHttpHandler = handler
            };

            var resolver = new CloudFoundryTokenKeyResolver(options);
            var result = resolver.ResolveSigningKey(token, null, "legacy-token-key", null);
            Assert.NotNull(handler.LastRequest);
            Assert.NotNull(resolver.Resolved["legacy-token-key"]);
            Assert.NotNull(result);
        }

        [Fact]
        public void ResolveSigningKey_IssuesHttpRequest_DoesntResolveKey()
        {
            string token = "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";
            string keyset = "{ 'keys':[{'kid':'foobar','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(keyset);
            handler.Response = response;
            CloudFoundryOptions options = new CloudFoundryOptions()
            {
                BackchannelHttpHandler = handler
            };

            var resolver = new CloudFoundryTokenKeyResolver(options);
            var result = resolver.ResolveSigningKey(token, null, "legacy-token-key", null);
            Assert.NotNull(handler.LastRequest);
            Assert.False(resolver.Resolved.ContainsKey("legacy-token-key"));
            Assert.Null(result);
        }

        [Fact]
        public async void FetchKeySet_IssuesHttpRequest_ReturnsKeyset()
        {
            string keyset = "{ 'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(keyset);
            handler.Response = response;
            CloudFoundryOptions options = new CloudFoundryOptions()
            {
                BackchannelHttpHandler = handler
            };

            var resolver = new CloudFoundryTokenKeyResolver(options);
            var result = await resolver.FetchKeySet();
            Assert.NotNull(result);
        }

        [Fact]
        public void GetJsonWebKey_DecodesValidJson()
        {
            var webKey = @"{'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
            CloudFoundryTokenKeyResolver resolver = new CloudFoundryTokenKeyResolver(new CloudFoundryOptions());
            var webKeySet = resolver.GetJsonWebKeySet(webKey);
            Assert.NotNull(webKeySet);
            Assert.NotNull(webKeySet.Keys);
            Assert.Equal(1, webKeySet.Keys.Count);
        }

        [Fact]
        public void GetHttpClient_AddsHandler()
        {
            TestMessageHandler handler = new TestMessageHandler();
            CloudFoundryOptions options = new CloudFoundryOptions()
            {
                BackchannelHttpHandler = handler
            };

            var resolver = new CloudFoundryTokenKeyResolver(options);
            var client = resolver.GetHttpClient();
            client.GetAsync("http://localhost/");
            Assert.NotNull(handler.LastRequest);
        }
    }

   
}
