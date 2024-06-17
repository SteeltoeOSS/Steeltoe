// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class TokenKeyResolverTest
{
    // ReSharper disable StringLiteralTypo
    private const string Token =
        "eyJhbGciOiJSUzI1NiIsImtpZCI6ImxlZ2FjeS10b2tlbi1rZXkiLCJ0eXAiOiJKV1QifQ.eyJqdGkiOiI0YjM2NmY4MDdlMjU0MzlmYmRkOTEwZDc4ZjcwYzlhMSIsInN1YiI6ImZlNmExYmUyLWM5MTEtNDM3OC05Y2MxLTVhY2Y1NjA1Y2ZjMiIsInNjb3BlIjpbImNsb3VkX2NvbnRyb2xsZXIucmVhZCIsImNsb3VkX2NvbnRyb2xsZXJfc2VydmljZV9wZXJtaXNzaW9ucy5yZWFkIiwidGVzdGdyb3VwIiwib3BlbmlkIl0sImNsaWVudF9pZCI6Im15VGVzdEFwcCIsImNpZCI6Im15VGVzdEFwcCIsImF6cCI6Im15VGVzdEFwcCIsImdyYW50X3R5cGUiOiJhdXRob3JpemF0aW9uX2NvZGUiLCJ1c2VyX2lkIjoiZmU2YTFiZTItYzkxMS00Mzc4LTljYzEtNWFjZjU2MDVjZmMyIiwib3JpZ2luIjoidWFhIiwidXNlcl9uYW1lIjoiZGF2ZSIsImVtYWlsIjoiZGF2ZSIsImF1dGhfdGltZSI6MTQ3MzYxNTU0MSwicmV2X3NpZyI6IjEwZDM1NzEyIiwiaWF0IjoxNDczNjI0MjU1LCJleHAiOjE0NzM2Njc0NTUsImlzcyI6Imh0dHBzOi8vdWFhLnN5c3RlbS50ZXN0Y2xvdWQuY29tL29hdXRoL3Rva2VuIiwiemlkIjoidWFhIiwiYXVkIjpbImNsb3VkX2NvbnRyb2xsZXIiLCJteVRlc3RBcHAiLCJvcGVuaWQiLCJjbG91ZF9jb250cm9sbGVyX3NlcnZpY2VfcGVybWlzc2lvbnMiXX0.Hth_SXpMAyiTf--U75r40qODlSUr60U730IW28K2VidEltW3lN3_CE7HkSjolRGr-DYuWHRvy3i_EwBfj1WTkBaXL373UzPVvNBnat9Gi-vjz07LwmBohk3baG1mmlL8IoGbQwtsmfUPhmO5C6_M4s9wKmTf9XIZPVo_w7zPJadrXfHLfx6iQob7CYpTTix2VBWya29iL7kmD1J1UDT5YRg2J9XT30iFuL6BvPQTkuGnX3ivDuUOSdxM8Z451i0VJmc0LYFBCLJ-Tz6bJ2d0wrtfsbCfuNtxjmGJevcL2jKQbEoiliYj60qNtZdT-ijGUdZjE9caxQ2nOkDkowacpw";

    private static readonly SecurityToken MockToken = new Mock<SecurityToken>().Object;
    private static readonly TokenValidationParameters MockParameters = new Mock<TokenValidationParameters>().Object;

    private const string KeySet = """
        {
          "keys": [
            {
              "kid": "legacy-token-key",
              "alg": "SHA256withRSA",
              "value": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----",
              "kty": "RSA",
              "use": "sig",
              "n": "AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=",
              "e": "AQAB"
            }
          ]
        }
        """;

    [Fact]
    public void ResolveSigningKey_FindsExistingKey()
    {
        var keys = JsonWebKeySet.Create(KeySet);
        JsonWebKey webKey = keys.Keys[0];
        TokenKeyResolver.ResolvedSecurityKeysById.Clear();
        using var httpClient = new HttpClient();
        var resolver = new TokenKeyResolver("https://foo.bar", httpClient);
        TokenKeyResolver.ResolvedSecurityKeysById["legacy-token-key"] = webKey;

        SecurityKey[] result = resolver.ResolveSigningKey(Token, MockToken, "legacy-token-key", MockParameters);

        result.Should().NotBeEmpty();
        result[0].Should().BeEquivalentTo(webKey);
    }

    [Fact]
    public void ResolveSigningKey_IssuesHttpRequest_ResolvesKey()
    {
        using var handler = new TestMessageHandler();

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(KeySet)
        };

        TokenKeyResolver.ResolvedSecurityKeysById.Clear();
        using var httpClient = new HttpClient(handler);
        var resolver = new TokenKeyResolver("https://foo.bar", httpClient);

        SecurityKey[] result = resolver.ResolveSigningKey(Token, MockToken, "legacy-token-key", MockParameters);

        handler.LastRequest.Should().NotBeNull();
        TokenKeyResolver.ResolvedSecurityKeysById.Keys.Should().Contain("legacy-token-key");
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveSigningKey_IssuesHttpRequest_DoesNotResolveKey()
    {
        const string alternateKeySet = """
            {
              "keys": [
                {
                  "kid": "foobar",
                  "alg": "SHA256withRSA",
                  "value": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----",
                  "kty": "RSA",
                  "use": "sig",
                  "n": "AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=",
                  "e": "AQAB"
                }
              ]
            }
            """;

        using var handler = new TestMessageHandler();

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(alternateKeySet)
        };

        TokenKeyResolver.ResolvedSecurityKeysById.Clear();
        using var httpClient = new HttpClient(handler);
        var resolver = new TokenKeyResolver("https://foo.bar", httpClient);

        SecurityKey[] result = resolver.ResolveSigningKey(Token, MockToken, "legacy-token-key", MockParameters);

        handler.LastRequest.Should().NotBeNull();
        TokenKeyResolver.ResolvedSecurityKeysById.Keys.Should().NotContain("legacy-token-key");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchKeySet_IssuesHttpRequest_ReturnsKeySet()
    {
        using var handler = new TestMessageHandler();

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(KeySet)
        };

        TokenKeyResolver.ResolvedSecurityKeysById.Clear();
        using var httpClient = new HttpClient(handler);
        var resolver = new TokenKeyResolver("https://foo.bar", httpClient);

        JsonWebKeySet? result = await resolver.FetchKeySetAsync(default);

        result.Should().NotBeNull();
        result!.Keys.Should().NotBeEmpty();
    }

    private sealed class TestMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }
}
