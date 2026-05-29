// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class TokenKeyResolverTest
{
    private const string EmptyKeySet = """
        {
          "keys": []
        }
        """;

    private const string KeySetWithKeyA = """
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

    private const string KeySetWithKeyB = """
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

    private const string KeySetWithBothKeys = """
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

    public static TheoryData<Exception> ServerUnreachableExceptions =>
    [
        new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused)),
        new TaskCanceledException("The request timed out.", new TimeoutException())
    ];

    [Fact]
    public void Fetches_existing_key_and_returns_it_from_cache()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(KeySetWithKeyA);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result1.Should().NotBeNull();
        result1.KeyId.Should().Be("key-a");

        timeProvider.Advance(TimeSpan.FromHours(11));
        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result2.Should().NotBeNull();
        result2.ToString().Should().Be(result1.ToString());

        handler.RequestCount.Should().Be(1);

        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Refetches_existing_key_after_expired_from_cache()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(KeySetWithKeyA);
        using var httpClient = new HttpClient(handler);

        _ = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);
        timeProvider.Advance(TimeSpan.FromHours(13));
        JsonWebKey? result = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result.Should().NotBeNull();
        result.KeyId.Should().Be("key-a");

        handler.RequestCount.Should().Be(2);

        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Returns_null_when_key_no_longer_present_after_refetch()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, NullLoggerFactory.Instance);
        using var handlerBoth = new TestMessageHandler(KeySetWithBothKeys);
        using var httpClientBoth = new HttpClient(handlerBoth);
        using var handlerB = new TestMessageHandler(KeySetWithKeyB);
        using var httpClientB = new HttpClient(handlerB);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientBoth);

        result1.Should().NotBeNull();

        timeProvider.Advance(TimeSpan.FromHours(13));
        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientB);

        result2.Should().BeNull();

        handlerBoth.RequestCount.Should().Be(1);
        handlerB.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Returns_key_from_refetch_after_it_became_available()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, NullLoggerFactory.Instance);
        using var handlerB = new TestMessageHandler(KeySetWithKeyB);
        using var httpClientB = new HttpClient(handlerB);
        using var handlerA = new TestMessageHandler(KeySetWithKeyA);
        using var httpClientA = new HttpClient(handlerA);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientB);

        result1.Should().BeNull();

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientA);

        result2.Should().NotBeNull();
        result2.KeyId.Should().Be("key-a");

        JsonWebKey? result3 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientA);

        result3.Should().NotBeNull();
        result3.ToString().Should().Be(result2.ToString());

        handlerB.RequestCount.Should().Be(1);
        handlerA.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Returns_existing_key_from_cache_if_fetched_other_key_earlier()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        using var resolver = new TokenKeyResolver(TimeProvider.System, loggerFactory);
        using var handler = new TestMessageHandler(KeySetWithBothKeys);
        using var httpClient = new HttpClient(handler);

        _ = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);
        JsonWebKey? result = resolver.ResolveSigningKey("https://server.com/path", "key-b", httpClient);

        result.Should().NotBeNull();
        result.KeyId.Should().Be("key-b");

        handler.RequestCount.Should().Be(1);

        loggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Fetches_unknown_key_and_returns_it_from_cache()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(EmptyKeySet);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "unknown-key", httpClient);

        result1.Should().BeNull();

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().StartWith($"INFO {typeof(TokenKeyResolver)}: Disabled fetch for key 'unknown-key' for ")
            .And.EndWith("s because the key was not found in the HTTP response.");

        loggerProvider.Clear();
        timeProvider.Advance(TimeSpan.FromSeconds(15));
        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "unknown-key", httpClient);

        result2.Should().BeNull();
        loggerProvider.GetAll().Should().BeEmpty();

        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Refetches_unknown_key_after_expired_from_cache()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(KeySetWithKeyA);
        using var httpClient = new HttpClient(handler);

        _ = resolver.ResolveSigningKey("https://server.com/path", "unknown-key", httpClient);
        loggerProvider.Clear();
        timeProvider.Advance(TimeSpan.FromSeconds(90));
        JsonWebKey? result = resolver.ResolveSigningKey("https://server.com/path", "unknown-key", httpClient);

        result.Should().BeNull();

        loggerProvider.GetAll().Should().ContainSingle().Which.Should().StartWith($"INFO {typeof(TokenKeyResolver)}: Disabled fetch for key 'unknown-key' for ")
            .And.EndWith("s because the key was not found in the HTTP response.");

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Normalizes_trailing_slash_in_authority()
    {
        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        using var handler = new TestMessageHandler(KeySetWithKeyA);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);
        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path/", "key-a", httpClient);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result2.ToString().Should().Be(result1.ToString());

        handler.RequestCount.Should().Be(1);
        handler.LastRequestUrl.Should().Be("https://server.com/path/token_keys");
    }

    [Fact]
    public void Uses_separate_cache_per_authority()
    {
        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        using var handler = new TestMessageHandler(KeySetWithKeyA);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result1.Should().NotBeNull();
        result1.KeyId.Should().Be("key-a");
        handler.RequestCount.Should().Be(1);

        JsonWebKey? result2 = resolver.ResolveSigningKey("https://other-server.com/alt-path", "key-a", httpClient);

        result2.Should().NotBeNull();
        result2.KeyId.Should().Be("key-a");
        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Uses_separate_cache_per_keyId()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, NullLoggerFactory.Instance);

        using var handlerA = new TestMessageHandler(KeySetWithKeyA);
        using var httpClientA = new HttpClient(handlerA);
        using var handlerB = new TestMessageHandler(KeySetWithKeyB);
        using var httpClientB = new HttpClient(handlerB);

        // t=0: cache A
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientA);

        handlerA.RequestCount.Should().Be(1);

        // t=11: cache B
        timeProvider.Advance(TimeSpan.FromHours(11));
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-b", httpClientB);

        handlerB.RequestCount.Should().Be(1);

        // t=13: A expired while B still cached
        timeProvider.Advance(TimeSpan.FromHours(2));
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientA);
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-b", httpClientB);

        handlerA.RequestCount.Should().Be(2);
        handlerB.RequestCount.Should().Be(1);
    }

    [Fact]
    public void All_keys_from_response_are_cached()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, NullLoggerFactory.Instance);
        using var handlerB = new TestMessageHandler(KeySetWithKeyB);
        using var httpClientB = new HttpClient(handlerB);
        using var handlerBoth = new TestMessageHandler(KeySetWithBothKeys);
        using var httpClientBoth = new HttpClient(handlerBoth);

        // t=0: cache B
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-b", httpClientB);

        handlerB.RequestCount.Should().Be(1);

        // t=11: cache A, re-cache B
        timeProvider.Advance(TimeSpan.FromHours(11));
        _ = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClientBoth);

        handlerBoth.RequestCount.Should().Be(1);

        // t=13: A and B still cached
        timeProvider.Advance(TimeSpan.FromHours(2));
        JsonWebKey? result = resolver.ResolveSigningKey("https://server.com/path", "key-b", httpClientB);

        result.Should().NotBeNull();
        result.KeyId.Should().Be("key-b");

        handlerB.RequestCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(ServerUnreachableExceptions))]
    public void Caches_shortly_when_server_is_unreachable(Exception exception)
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(exception);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result1.Should().BeNull();

        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result2.Should().BeNull();

        handler.RequestCount.Should().Be(1);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().HaveCount(2);

        logLines[0].Should().Be($"WARN {typeof(TokenKeyResolver)}: Fetch keys from 'https://server.com/path/token_keys' failed.");

        logLines[1].Should().StartWith($"INFO {typeof(TokenKeyResolver)}: Disabled fetch for key 'key-a' for ").And
            .EndWith("s because the HTTP request failed.");

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        JsonWebKey? result3 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result3.Should().BeNull();

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
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler(statusCode);
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result1.Should().BeNull();

        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result2.Should().BeNull();

        handler.RequestCount.Should().Be(1);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().HaveCount(2);

        logLines[0].Should().Be(
            $"WARN {typeof(TokenKeyResolver)}: Fetch keys from 'https://server.com/path/token_keys' failed with HTTP status {(int)statusCode}.");

        logLines[1].Should().StartWith($"INFO {typeof(TokenKeyResolver)}: Disabled fetch for key 'key-a' for ").And
            .EndWith("s because the HTTP request failed.");

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        JsonWebKey? result3 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result3.Should().BeNull();

        handler.RequestCount.Should().Be(2);
    }

    [Fact]
    public void Caches_shortly_when_server_returns_broken_JSON()
    {
        using var loggerProvider = new CapturingLoggerProvider((_, level) => level >= LogLevel.Information);
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        var timeProvider = new FakeTimeProvider();
        using var resolver = new TokenKeyResolver(timeProvider, loggerFactory);
        using var handler = new TestMessageHandler("{");
        using var httpClient = new HttpClient(handler);

        JsonWebKey? result1 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result1.Should().BeNull();

        JsonWebKey? result2 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result2.Should().BeNull();

        handler.RequestCount.Should().Be(1);

        IList<string> logLines = loggerProvider.GetAll();
        logLines.Should().HaveCount(2);

        logLines[0].Should().Be(
            $"WARN {typeof(TokenKeyResolver)}: Fetch keys from 'https://server.com/path/token_keys' failed because the returned JSON is invalid.");

        logLines[1].Should().StartWith($"INFO {typeof(TokenKeyResolver)}: Disabled fetch for key 'key-a' for ").And
            .EndWith("s because the HTTP request failed.");

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        JsonWebKey? result3 = resolver.ResolveSigningKey("https://server.com/path", "key-a", httpClient);

        result3.Should().BeNull();

        handler.RequestCount.Should().Be(2);
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
}
