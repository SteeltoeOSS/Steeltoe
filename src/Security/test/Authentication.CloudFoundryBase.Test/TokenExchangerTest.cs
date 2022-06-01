// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using RichardSzalay.MockHttp;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class TokenExchangerTest
{
    [Fact]
    public void GetTokenRequestMessage_ReturnsCorrectly()
    {
        var opts = new AuthServerOptions { ClientId = "clientId", ClientSecret = "clientSecret" };
        var tEx = new TokenExchanger(opts);

        var message = tEx.GetTokenRequestMessage(new List<KeyValuePair<string, string>>(), "redirectUri");

        Assert.NotNull(message);
        var content = message.Content as FormUrlEncodedContent;
        Assert.NotNull(content);
        Assert.Equal(HttpMethod.Post, message.Method);

        Assert.Contains(new MediaTypeWithQualityHeaderValue("application/json"), message.Headers.Accept);
    }

    [Fact]
    public void AuthCodeTokenRequestParameters_ReturnsCorrectly()
    {
        var opts = new AuthServerOptions { ClientId = "clientId", ClientSecret = "clientSecret", CallbackUrl = "redirect_uri" };
        var tEx = new TokenExchanger(opts);

        var parameters = tEx.AuthCodeTokenRequestParameters("authcode");

        Assert.NotNull(parameters);
        Assert.Equal(opts.ClientId, parameters.First(i => i.Key == "client_id").Value);
        Assert.Equal(opts.ClientSecret, parameters.First(i => i.Key == "client_secret").Value);
        Assert.Equal("redirect_uri", parameters.First(i => i.Key == "redirect_uri").Value);
        Assert.Equal("authcode", parameters.First(i => i.Key == "code").Value);
        Assert.Equal(OpenIdConnectGrantTypes.AuthorizationCode, parameters.First(i => i.Key == "grant_type").Value);
    }

    [Fact]
    public void ClientCredentialsTokenRequestParameters_ReturnsCorrectly()
    {
        var opts = new AuthServerOptions { ClientId = "clientId", ClientSecret = "clientSecret", CallbackUrl = "redirect_uri" };
        var tEx = new TokenExchanger(opts);

        var parameters = tEx.ClientCredentialsTokenRequestParameters();

        Assert.NotNull(parameters);
        Assert.Equal(opts.ClientId, parameters.First(i => i.Key == "client_id").Value);
        Assert.Equal(opts.ClientSecret, parameters.First(i => i.Key == "client_secret").Value);
        Assert.Equal(OpenIdConnectGrantTypes.ClientCredentials, parameters.First(i => i.Key == "grant_type").Value);
    }

    [Fact]
    public void CommonTokenRequestParamsHandlesScopes()
    {
        var opts = new AuthServerOptions { AdditionalTokenScopes = "onescope", RequiredScopes = new[] { "twoscope" } };
        var tEx = new TokenExchanger(opts);

        var parameters = tEx.CommonTokenRequestParams();
        Assert.Equal("openid onescope twoscope", parameters.First(i => i.Key == CloudFoundryDefaults.ParamsScope).Value);
    }

    [Fact]
    public async Task ExchangeAuthCodeForClaimsIdentity_ExchangesCodeForIdentity()
    {
        var options = new AuthServerOptions
        {
            AuthorizationUrl = "http://localhost/tokenUrl"
        };
        var exchanger = new TokenExchanger(options, GetMockHttpClient());

        var identity = await exchanger.ExchangeAuthCodeForClaimsIdentity("goodCode");

        Assert.IsType<ClaimsIdentity>(identity);
    }

    [Fact]
    public async Task ExchangeAuthCodeForClaimsIdentity_ReturnsNullOnFailure()
    {
        var options = new AuthServerOptions
        {
            AuthorizationUrl = "http://localhost/tokenUrl"
        };
        var httpClient = new object(); // TODO: replace with mock that does stuff
        var exchanger = new TokenExchanger(options, GetMockHttpClient());

        var identity = await exchanger.ExchangeAuthCodeForClaimsIdentity("badCode");

        Assert.Null(identity);
    }

    private readonly string realTokenResponse = @"{""access_token"":""eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJqdGkiOiI3YTMzYzVhNjhjY2I0YjRiYmQ5N2I4MTRlZWExMTc3MiIsInN1YiI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsInNjb3BlIjpbInRlc3Rncm91cCIsIm9wZW5pZCJdLCJjbGllbnRfaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJjaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJhenAiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJncmFudF90eXBlIjoiYXV0aG9yaXphdGlvbl9jb2RlIiwidXNlcl9pZCI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsIm9yaWdpbiI6InVhYSIsInVzZXJfbmFtZSI6ImRhdmUiLCJlbWFpbCI6ImRhdmVAdGVzdGNsb3VkLmNvbSIsImF1dGhfdGltZSI6MTU0NjY0MjkzMywicmV2X3NpZyI6ImE1ZWY2ODg5IiwiaWF0IjoxNTQ2NjQyOTM1LCJleHAiOjE1NDY2ODYxMzUsImlzcyI6Imh0dHBzOi8vc3RlZWx0b2UudWFhLmNmLmJlZXQuc3ByaW5nYXBwcy5pby9vYXV0aC90b2tlbiIsInppZCI6IjNhM2VhZGFkLTViMmYtNDUzMC1hZjk1LWE2OWJjMGFmZDE1YiIsImF1ZCI6WyJvcGVuaWQiLCJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiXX0.tGTXZzuuUSObTwdPHSx-zvnld20DH5hlOZlYp5DhjwkMIsZB0uIvVwbVDkPp7H_AmmeJoo6vqa5hbbgfgnYpTrKlCGOypnHoa3yRIKrwcDmLLujaMz6ApZeaJ7sJN-0N1UnPZ9iGcqvt9hNb_198zRnMXGH72oI0e2iGUBV1olCFVdZTnMGT7sUieDFKy7n0ghZYq_gUI8rfvTwiC3lfxv0nDXz4oE9Z-UKhK6q1zkAtQrz61FQ_CHONejz1JnuxQFKMMvm8JLcRkn6OL-EcSi1hkmFw0efO1OqccQacxphlafyHloVPQ3IOtzLjCf8sJ5NgTdCTC3iddT_sYovdrg"",""token_type"":""bearer"",""id_token"":""eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJzdWIiOiI5NWJiYjM0Ni1iNjhjLTRmMTUtYjM0MS03MGQ2MGY5ZjQ2YmEiLCJhdWQiOlsiYzkyMGI0ZjUtNDg3Yy00ZGQwLWE2M2QtNGQ0MGMxMzMxOTg2Il0sImlzcyI6Imh0dHBzOi8vc3RlZWx0b2UudWFhLmNmLmJlZXQuc3ByaW5nYXBwcy5pby9vYXV0aC90b2tlbiIsImV4cCI6MTU0NjY4NjEzNSwiaWF0IjoxNTQ2NjQyOTM1LCJhbXIiOlsicHdkIl0sImF6cCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsInNjb3BlIjpbIm9wZW5pZCJdLCJlbWFpbCI6ImRhdmVAdGVzdGNsb3VkLmNvbSIsInppZCI6IjNhM2VhZGFkLTViMmYtNDUzMC1hZjk1LWE2OWJjMGFmZDE1YiIsIm9yaWdpbiI6InVhYSIsImp0aSI6IjdhMzNjNWE2OGNjYjRiNGJiZDk3YjgxNGVlYTExNzcyIiwicHJldmlvdXNfbG9nb25fdGltZSI6MTU0NjYzMzU1NjA0NCwiZW1haWxfdmVyaWZpZWQiOnRydWUsImNsaWVudF9pZCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsImNpZCI6ImM5MjBiNGY1LTQ4N2MtNGRkMC1hNjNkLTRkNDBjMTMzMTk4NiIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX25hbWUiOiJkYXZlIiwicmV2X3NpZyI6ImE1ZWY2ODg5IiwidXNlcl9pZCI6Ijk1YmJiMzQ2LWI2OGMtNGYxNS1iMzQxLTcwZDYwZjlmNDZiYSIsImF1dGhfdGltZSI6MTU0NjY0MjkzM30.KkTVOTg7Bhj1EWO63QmWzjnEAnKesoSLGfGL-2Y19PiK62KRd66dOcVcQEA_nWIE1mJQZsDByQYwcEuVRAiP-mXY0L2MrWUnRlW5yn1fqOc44iSDggMF5VfjGQok8fGfBPQX7va0evfaOaulRMuWsijYvzZtV-KncGUpxGwkzRs2AAEbkAv1_vAD2zSGJ-ji5L7s4a2-Qc_LxDlNANoYllzMTVxZ2DSvVPLfKPNGgSvNC7t053ExfGRXk-6cPxVznkngWDlYALeXsnrbvXdjuk1dw8dcXRhL4PUJDI7EVvTdqzd1fPYRgAQ3KJZOmvzBY7bxFtoq9odmKKHTI4CFUQ"",""refresh_token"":""eyJhbGciOiJSUzI1NiIsImtpZCI6ImtleS0xIiwidHlwIjoiSldUIn0.eyJqdGkiOiI4NjdiNTcxNTBlNmM0ZDQ3OTM0NmE3ZjgwMTJmMGY2My1yIiwic3ViIjoiOTViYmIzNDYtYjY4Yy00ZjE1LWIzNDEtNzBkNjBmOWY0NmJhIiwic2NvcGUiOlsidGVzdGdyb3VwIiwib3BlbmlkIl0sImlhdCI6MTU0NjY0MjkzNSwiZXhwIjoxNTQ5MjM0OTM1LCJjaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJjbGllbnRfaWQiOiJjOTIwYjRmNS00ODdjLTRkZDAtYTYzZC00ZDQwYzEzMzE5ODYiLCJpc3MiOiJodHRwczovL3N0ZWVsdG9lLnVhYS5jZi5iZWV0LnNwcmluZ2FwcHMuaW8vb2F1dGgvdG9rZW4iLCJ6aWQiOiIzYTNlYWRhZC01YjJmLTQ1MzAtYWY5NS1hNjliYzBhZmQxNWIiLCJncmFudF90eXBlIjoiYXV0aG9yaXphdGlvbl9jb2RlIiwidXNlcl9uYW1lIjoiZGF2ZSIsIm9yaWdpbiI6InVhYSIsInVzZXJfaWQiOiI5NWJiYjM0Ni1iNjhjLTRmMTUtYjM0MS03MGQ2MGY5ZjQ2YmEiLCJyZXZfc2lnIjoiYTVlZjY4ODkiLCJhdWQiOlsib3BlbmlkIiwiYzkyMGI0ZjUtNDg3Yy00ZGQwLWE2M2QtNGQ0MGMxMzMxOTg2Il19.cOHCiZgmbtN1uyuvEou879du5XVvJqHEmkf6-2G0V9I1qYy0PJbGMHNL32d6L9LVsnXs4iTfi1qpKfdEGbfZbX8rNwNwcRoPndOZBepTTZHJPcU49VEF4t1hZ5icbt_4mSiQwpNy2n_mqwHizV8BAeOeQc_zUE-YFiuQC6V3ac0rTYO4NJGSioSG_y6HFp3refiPZVPT7En-dwhd-Yic1SB1OPxQ5bRNS7AAjGLeeCWOZQVxwQa5Atv9t791yUkH1zX8Psh_LRy2O8E6o7IBLI_yjz5N-YIHRa-BuMPDdKWarcOjy1KKJM27HynAQo1T4J0Ft8hSsIxFlObTd3-FVQ"",""expires_in"":43199,""scope"":""testgroup openid"",""jti"":""7a33c5a68ccb4b4bbd97b814eea11772""}";

    private HttpClient GetMockHttpClient()
    {
        var mockHttp = new MockHttpMessageHandler();

        mockHttp
            .When("http://localhost/tokenUrl")
            .WithFormData("code", "goodCode")
            .Respond("application/json", realTokenResponse); // Respond with JSON
        mockHttp
            .When("http://localhost/tokenUrl")
            .WithFormData("code", "badCode")
            .Respond(HttpStatusCode.BadRequest); // Respond with JSON

        return mockHttp.ToHttpClient();
    }
}
