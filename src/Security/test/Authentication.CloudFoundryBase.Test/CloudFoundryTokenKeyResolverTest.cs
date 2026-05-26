// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Time.Testing;
using Steeltoe.Security.Authentication.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundryBase.Test;

public sealed class CloudFoundryTokenKeyResolverTest
{
    private const string _emptyKeySet = """
        {
          "keys": []
        }
        """;

    private const string _keySetWithKeyA = """
        {
          "keys": [
            {
              "kid": "key-a",
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

    private const string _keySetWithKeyB = """
        {
          "keys": [
            {
              "kid": "key-b",
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

    private const string _keySetWithBothKeys = """
        {
          "keys": [
            {
              "kid": "key-a",
              "alg": "SHA256withRSA",
              "value": "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----",
              "kty": "RSA",
              "use": "sig",
              "n": "AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=",
              "e": "AQAB"
            },
            {
              "kid": "key-b",
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

    public static IEnumerable<object[]> ServerUnreachableExceptions => new List<object[]>
    {
        new object[]
        {
            new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused)),
        },
        new object[]
        {
            new TaskCanceledException("The request timed out.", new TimeoutException())
        }
    };

    [Fact]
    public void Fetches_existing_key_and_returns_it_from_cache()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(_keySetWithKeyA);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().ContainSingle().Which.KeyId.Should().Be("key-a");

        timeProvider.Advance(TimeSpan.FromHours(11));
        var result2 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().ContainSingle().Which.ToString().Should().Be(result1[0].ToString());

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Refetches_existing_key_after_expired_from_cache()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(_keySetWithKeyA);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        _ = resolver.ResolveSigningKey(null, null, "key-a", null);
        timeProvider.Advance(TimeSpan.FromHours(13));
        var result = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result.Should().ContainSingle().Which.KeyId.Should().Be("key-a");

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Returns_empty_when_key_no_longer_present_after_refetch()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handlerBoth = new TestMessageHandler(_keySetWithBothKeys);
        using var handlerB = new TestMessageHandler(_keySetWithKeyB);
        var resolverBoth = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerBoth, false);
        var resolverB = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerB, false);

        var result1 = resolverBoth.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().ContainSingle().Which.Should().NotBeNull();

        timeProvider.Advance(TimeSpan.FromHours(13));
        var result2 = resolverB.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().BeEmpty();

        handlerBoth.RequestCount.Should().Be(1);
        handlerB.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Returns_key_from_refetch_after_it_became_available()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handlerB = new TestMessageHandler(_keySetWithKeyB);
        using var handlerA = new TestMessageHandler(_keySetWithKeyA);
        var resolverB = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerB, false);
        var resolverA = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerA, false);

        var result1 = resolverB.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().BeEmpty();

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        var result2 = resolverA.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().ContainSingle().Which.KeyId.Should().Be("key-a");

        var result3 = resolverA.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result3.Should().ContainSingle().Which.ToString().Should().Be(result2[0].ToString());

        handlerB.RequestCount.Should().Be(1);
        handlerA.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Returns_existing_key_from_cache_if_fetched_other_key_earlier()
    {
        CloudFoundryTokenKeyResolver.Cache.Clear();

        using var handler = new TestMessageHandler(_keySetWithBothKeys);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        _ = resolver.ResolveSigningKey(null, null, "key-a", null);
        var result = resolver.ResolveSigningKey(null, null, "key-b", null).ToArray();

        result.Should().ContainSingle().Which.KeyId.Should().Be("key-b");

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Fetches_unknown_key_and_returns_it_from_cache()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(_emptyKeySet);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "unknown-key", null).ToArray();

        result1.Should().BeEmpty();

        timeProvider.Advance(TimeSpan.FromSeconds(15));
        var result2 = resolver.ResolveSigningKey(null, null, "unknown-key", null).ToArray();

        result2.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Refetches_unknown_key_after_expired_from_cache()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(_keySetWithKeyA);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        _ = resolver.ResolveSigningKey(null, null, "unknown-key", null);
        timeProvider.Advance(TimeSpan.FromSeconds(90));
        var result = resolver.ResolveSigningKey(null, null, "unknown-key", null);

        result.Should().BeEmpty();
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Uses_separate_cache_per_authority()
    {
        CloudFoundryTokenKeyResolver.Cache.Clear();

        using var handler = new TestMessageHandler(_keySetWithKeyA);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);
        var altResolver = new CloudFoundryTokenKeyResolver("https://other-server.com/alt-path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().ContainSingle().Which.KeyId.Should().Be("key-a");
        handler.RequestCount.Should().Be(1);

        var result2 = altResolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().ContainSingle().Which.KeyId.Should().Be("key-a");
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Uses_separate_cache_per_keyId()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handlerA = new TestMessageHandler(_keySetWithKeyA);
        using var handlerB = new TestMessageHandler(_keySetWithKeyB);
        var resolverA = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerA, false);
        var resolverB = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerB, false);

        // t=0: cache A
        _ = resolverA.ResolveSigningKey(null, null, "key-a", null);

        handlerA.RequestCount.Should().Be(1);

        // t=11: cache B
        timeProvider.Advance(TimeSpan.FromHours(11));
        _ = resolverB.ResolveSigningKey(null, null, "key-b", null);

        handlerB.RequestCount.Should().Be(1);

        // t=13: A expired while B still cached
        timeProvider.Advance(TimeSpan.FromHours(2));
        _ = resolverA.ResolveSigningKey(null, null, "key-a", null);
        _ = resolverB.ResolveSigningKey(null, null, "key-b", null);

        handlerA.RequestCount.Should().Be(2);
        handlerB.RequestCount.Should().Be(1);
    }

    [Fact]
    public void All_keys_from_response_are_cached()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handlerB = new TestMessageHandler(_keySetWithKeyB);
        using var handlerBoth = new TestMessageHandler(_keySetWithBothKeys);
        var resolverB = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerB, false);
        var resolverBoth = new CloudFoundryTokenKeyResolver("https://server.com/path", handlerBoth, false);

        // t=0: cache B
        _ = resolverB.ResolveSigningKey(null, null, "key-b", null);

        handlerB.RequestCount.Should().Be(1);

        // t=11: cache A, re-cache B
        timeProvider.Advance(TimeSpan.FromHours(11));
        _ = resolverBoth.ResolveSigningKey(null, null, "key-a", null);

        handlerBoth.RequestCount.Should().Be(1);

        // t=13: A and B still cached
        timeProvider.Advance(TimeSpan.FromHours(2));
        var result = resolverB.ResolveSigningKey(null, null, "key-b", null).ToArray();

        result.Should().ContainSingle().Which.KeyId.Should().Be("key-b");

        handlerB.RequestCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(ServerUnreachableExceptions))]
    public void Caches_shortly_when_server_is_unreachable(Exception exception)
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(exception);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().BeEmpty();

        var result2 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        var result3 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result3.Should().BeEmpty();

        handler.RequestCount.Should().Be(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public void Caches_shortly_when_server_returns_error(HttpStatusCode statusCode)
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler(statusCode);
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().BeEmpty();

        var result2 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        var result3 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result3.Should().BeEmpty();

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Caches_shortly_when_server_returns_broken_JSON()
    {
        var timeProvider = new FakeTimeProvider();
        using var scope = new MemoryCacheScope(timeProvider);

        using var handler = new TestMessageHandler("{");
        var resolver = new CloudFoundryTokenKeyResolver("https://server.com/path", handler, false);

        var result1 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result1.Should().BeEmpty();

        var result2 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result2.Should().BeEmpty();
        handler.RequestCount.Should().Be(1);

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        var result3 = resolver.ResolveSigningKey(null, null, "key-a", null).ToArray();

        result3.Should().BeEmpty();

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void GetJsonWebKey_DecodesValidJson()
    {
        CloudFoundryTokenKeyResolver.Cache.Clear();

        var webKey = @"{'keys':[{'kid':'legacy-token-key','alg':'SHA256withRSA','value':'-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAk+7xH35bYBppsn54cBW+\nFlrveTe+3L4xl7ix13XK8eBcCmNOyBhNzhks6toDiRjrgw5QW76cFirVRFIVQkiZ\nsUwDyGOax3q8NOJyBFXiplIUScrx8aI0jkY/Yd6ixAc5yBSBfXThy4EF9T0xCyt4\nxWLYNXMRwe88Y+i+MEoLNXWRbhjJm76LN7rsdIxALbS0vJNWUDALWjtE6FeYX6uU\nL9msAzlCQkdnSvwMmr8Ij2O3IVMxHDJXOZinFqt9zVfXwO11o7ZmiskZnRz1/V0f\nvbUQAadkcDEUt1gk9cbrAhiipg8VWDMsC7VUXuekJZjme5f8oWTwpsgP6cTUzwSS\n6wIDAQAB\n-----END PUBLIC KEY-----','kty':'RSA','use':'sig','n':'AJPu8R9+W2AaabJ+eHAVvhZa73k3vty+MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu+nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQHOcgUgX104cuBBfU9MQsreMVi2DVzEcHvPGPovjBKCzV1kW4YyZu+ize67HSMQC20tLyTVlAwC1o7ROhXmF+rlC/ZrAM5QkJHZ0r8DJq/CI9jtyFTMRwyVzmYpxarfc1X18DtdaO2ZorJGZ0c9f1dH721EAGnZHAxFLdYJPXG6wIYoqYPFVgzLAu1VF7npCWY5nuX/KFk8KbID+nE1M8Ekus=','e':'AQAB'}]}";
        var resolver = new CloudFoundryTokenKeyResolver("https://foo.bar", null, false);
        var webKeySet = resolver.GetJsonWebKeySet(FixBrokenJson(webKey));

        Assert.NotNull(webKeySet);
        Assert.NotNull(webKeySet.Keys);
        Assert.Equal(1, webKeySet.Keys.Count);
    }

    [Fact]
    public void GetHttpClient_AddsHandler()
    {
        var handler = new TestMessageHandler(HttpStatusCode.OK);

        var resolver = new CloudFoundryTokenKeyResolver("https://foo.bar", handler, false);
        var client = resolver.GetHttpClient();
        client.GetAsync("http://localhost/");
        Assert.NotNull(handler.LastRequestUrl);
    }

    [Fact]
    public void HttpClient_HasAtLeast_Default100secondsTimeout()
    {
        var resolver = new CloudFoundryTokenKeyResolver("https://foo.bar", null, false);
        var client = resolver.GetHttpClient();

        Assert.True(client.Timeout >= TimeSpan.FromSeconds(100));
    }

    private static string FixBrokenJson(string json)
    {
        return json.Replace('\'', '"').Replace("\n", "\\n");
    }

    private sealed class TestMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _responseStatusCode = HttpStatusCode.OK;
        private readonly string _responseText = string.Empty;
        private readonly Exception? _exceptionToThrow;

        public int RequestCount { get; private set; }

        public string? LastRequestUrl { get; private set; }

        public TestMessageHandler(string responseText)
        {
            _responseText = responseText;
        }

        public TestMessageHandler(HttpStatusCode statusCode)
        {
            _responseStatusCode = statusCode;
        }

        public TestMessageHandler(Exception exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUrl = request.RequestUri?.ToString();

            if (_exceptionToThrow != null)
            {
                return Task.FromException<HttpResponseMessage>(_exceptionToThrow);
            }

            var response = new HttpResponseMessage(_responseStatusCode)
            {
                Content = new StringContent(_responseText)
            };

            return Task.FromResult(response);
        }
    }

    private sealed class MemoryCacheScope : IDisposable
    {
        private readonly MemoryCache _backupCache;

        public MemoryCacheScope(TimeProvider timeProvider)
        {
            _backupCache = CloudFoundryTokenKeyResolver.Cache;
            CloudFoundryTokenKeyResolver.Cache = new MemoryCache(new MemoryCacheOptions()
            {
                Clock = new TimeProviderSystemClock(timeProvider)
            });
        }

        public void Dispose()
        {
            CloudFoundryTokenKeyResolver.Cache = _backupCache;
        }

        private sealed class TimeProviderSystemClock : ISystemClock
        {
            private readonly TimeProvider _timeProvider;

            public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

            public TimeProviderSystemClock(TimeProvider timeProvider)
            {
                ArgumentNullException.ThrowIfNull(timeProvider);
                _timeProvider = timeProvider;
            }
        }
    }
}