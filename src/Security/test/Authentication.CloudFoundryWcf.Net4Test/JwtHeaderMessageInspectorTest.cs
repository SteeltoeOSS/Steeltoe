// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using RichardSzalay.MockHttp;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class JwtHeaderMessageInspectorTest
    {
        [Fact]
        public void MessageInspector_AttachesUserToken()
        {
            // arrange
            var options = new CloudFoundryOptions() { AuthorizationUrl = "http://localhost", ForwardUserCredentials = true };
            var inspector = new JwtHeaderMessageInspector(options, "someToken");
            var properties = new MessageProperties { { HttpRequestMessageProperty.Name, new HttpRequestMessageProperty() } };
            var message = new Mock<Message>();
            message.Setup(p => p.Properties).Returns(() => properties);
            var mo = message.Object;

            // act
            inspector.BeforeSendRequest(ref mo, null);
            HttpRequestMessageProperty httpRequestMessage;
            mo.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject);
            httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;

            // assert
            Assert.True(httpRequestMessage.Headers.AllKeys.Any());
            Assert.Equal("Bearer someToken", httpRequestMessage.Headers["Authorization"]);
        }

        [Fact]
        public void MessageInspector_GetsAndAttachesOwnToken()
        {
            // arrange
            var options = new CloudFoundryOptions() { AccessTokenEndpoint = "/tokenUrl", AuthorizationUrl = "http://localhost", ClientId = "validId", ClientSecret = "validSecret" };
            var inspector = new JwtHeaderMessageInspector(options, null, GetMockHttpClient());
            var properties = new MessageProperties { { HttpRequestMessageProperty.Name, new HttpRequestMessageProperty() } };
            var message = new Mock<Message>();
            message.Setup(p => p.Properties).Returns(() => properties);
            var mo = message.Object;

            // act
            inspector.BeforeSendRequest(ref mo, null);
            HttpRequestMessageProperty httpRequestMessage;
            mo.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject);
            httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;

            // assert
            Assert.True(httpRequestMessage.Headers.AllKeys.Any());
            Assert.Equal("Bearer someClientCredentialsToken", httpRequestMessage.Headers["Authorization"]);
        }

        private readonly string realTokenResponse = "{'access_token':'someClientCredentialsToken','token_type':'bearer','id_token':'eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJzdWIiOiI5NWJiYjM0Ni1iNjhjLTRmMTUtYjM0MS03MGQ2MGY5ZjQ2YmEiLCJhdWQiOlsiYzkyMGI0ZjUtNDg3Yy00ZGQwLWE2M2QtNGQ0MGMxMzMxOTg2Il0sImlzcyI6Imh0dHBzOi8vc3RlZWx0b2UudWFhLmNmLmJlZXQuc3ByaW5nYXBwcy5pby9vYXV0aC90b2tlbiIsImV4cCI6MTU0NjY4NjEzNSwiaWF0IjoxNTQ2NjQyOTM1LCJhbXIiOlsicHdkIl0sImF6cCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsInNjb3BlIjpbIm9wZW5pZCJdLCJlbWFpbCI6ImRhdmVAdGVzdGNsb3VkLmNvbSIsInppZCI6IjNhM2VhZGFkLTViMmYtNDUzMC1hZjk1LWE2OWJjMGFmZDE1YiIsIm9yaWdpbiI6InVhYSIsImp0aSI6IjdhMzNjNWE2OGNjYjRiNGJiZDk3YjgxNGVlYTExNzcyIiwicHJldmlvdXNfbG9nb25fdGltZSI6MTU0NjYzMzU1NjA0NCwiZW1haWxfdmVyaWZpZWQiOnRydWUsImNsaWVudF9pZCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsImNpZCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX25hbWUiOiJkYXZlIiwicmV2X3NpZyI6ImE1ZWY2ODg5IiwidXNlcl9pZCI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsImF1dGhfdGltZSI6MTU0NjY0MjkzM30.KkTVOTg7Bhj1EWO63QmWzjnEAnKesoSLGfGL-2Y19PiK62KRd66dOcVcQEA_nWIE1mJQZsDByQYwcEuVRAiP-mXY0L2MrWUnRlW5yn1fqOc44iSDggMF5VfjGQok8fGfBPQX7va0evfaOaulRMuWsijYvzZtV-KncGUpxGwkzRs2AAEbkAv1_vAD2zSGJ-ji5L7s4a2-Qc_LxDlNANoYllzMTVxZ2DSvVPLfKPNGgSvNC7t053ExfGRXk-6cPxVznkngWDlYALeXsnrbvXdjuk1dw8dcXRhL4PUJDI7EVvTdqzd1fPYRgAQ3KJZOmvzBY7bxFtoq9odmKKHTI4CFUQ','refresh_token':'eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJqdGkiOiI4NjdiNTcxNTBlNmM0ZDQ3OTM0NmE3ZjgwMTJmMGY2My1yIiwic3ViIjoiOTViYmIzNDYtYjY4Yy00ZjE1LWIzNDEtNzBkNjBmOWY0NmJhIiwic2NvcGUiOlsidGVzdGdyb3VwIiwib3BlbmlkIl0sImlhdCI6MTU0NjY0MjkzNSwiZXhwIjoxNTQ5MjM0OTM1LCJjaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJjbGllbnRfaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJpc3MiOiJodHRwczovL3N0ZWVsdG9lLnVhYS5jZi5iZWV0LnNwcmluZ2FwcHMuaW8vb2F1dGgvdG9rZW4iLCJ6aWQiOiIzYTNlYWRhZC01YjJmLTQ1MzAtYWY5NS1hNjliYzBhZmQxNWIiLCJncmFudF90eXBlIjoiYXV0aG9yaXphdGlvbl9jb2RlIiwidXNlcl9uYW1lIjoiZGF2ZSIsIm9yaWdpbiI6InVhYSIsInVzZXJfaWQiOiI5NWJiYjM0Ni1iNjhjLTRmMTUtYjM0MS03MGQ2MGY5ZjQ2YmEiLCJyZXZfc2lnIjoiYTVlZjY4ODkiLCJhdWQiOlsib3BlbmlkIiwiYzkyMGI0ZjUtNDg3Yy00ZGQwLWE2M2QtNGQ0MGMxMzMxOTg2Il19.cOHCiZgmbtN1uyuvEou879du5XVvJqHEmkf6-2G0V9I1qYy0PJbGMHNL32d6L9LVsnXs4iTfi1qpKfdEGbfZbX8rNwNwcRoPndOZBepTTZHJPcU49VEF4t1hZ5icbt_4mSiQwpNy2n_mqwHizV8BAeOeQc_zUE-YFiuQC6V3ac0rTYO4NJGSioSG_y6HFp3refiPZVPT7En-dwhd-Yic1SB1OPxQ5bRNS7AAjGLeeCWOZQVxwQa5Atv9t791yUkH1zX8Psh_LRy2O8E6o7IBLI_yjz5N-YIHRa-BuMPDdKWarcOjy1KKJM27HynAQo1T4J0Ft8hSsIxFlObTd3-FVQ','expires_in':43199,'scope':'testgroup openid','jti':'7a33c5a68ccb4b4bbd97b814eea11772'}";

        private HttpClient GetMockHttpClient()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp
                .When("http://localhost/tokenUrl")
                .WithFormData("client_id", "validId")
                .Respond("application/json", realTokenResponse); // Respond with JSON

            return mockHttp.ToHttpClient();
        }
    }
}
