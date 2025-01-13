// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.IdentityModel.Tokens;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class TokenKeyResolverTest
{
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

        SecurityKey[] result = resolver.ResolveSigningKey("legacy-token-key");

        result.Should().NotBeEmpty();
        result[0].Should().Be(webKey);
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

        SecurityKey[] result = resolver.ResolveSigningKey("legacy-token-key");

        handler.LastRequest.Should().NotBeNull();
        TokenKeyResolver.ResolvedSecurityKeysById.Should().ContainKey("legacy-token-key");
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void ResolveSigningKey_IssuesHttpRequest_DoesNotResolveKey()
    {
        // ReSharper disable StringLiteralTypo
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
        // ReSharper restore StringLiteralTypo

        using var handler = new TestMessageHandler();

        handler.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(alternateKeySet)
        };

        TokenKeyResolver.ResolvedSecurityKeysById.Clear();
        using var httpClient = new HttpClient(handler);
        var resolver = new TokenKeyResolver("https://foo.bar", httpClient);

        SecurityKey[] result = resolver.ResolveSigningKey("legacy-token-key");

        handler.LastRequest.Should().NotBeNull();
        TokenKeyResolver.ResolvedSecurityKeysById.Should().NotContainKey("legacy-token-key");
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

        JsonWebKeySet? result = await resolver.FetchKeySetAsync(CancellationToken.None);

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
