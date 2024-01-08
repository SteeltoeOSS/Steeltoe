// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using k8s;
using k8s.Autorest;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.Test;

public sealed class KubernetesSecretProviderTest
{
    [Fact]
    public void KubernetesSecretProvider_ThrowsOn403()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.Forbidden);
        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());
        var provider = new KubernetesSecretProvider(client, settings, CancellationToken.None);

        var exception = Assert.Throws<HttpOperationException>(provider.Load);

        Assert.Equal(HttpStatusCode.Forbidden, exception.Response.StatusCode);
    }

    [Fact]
    public async Task KubernetesSecretProvider_ContinuesOn404()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);
        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings
        {
            ConfigMaps = true,
            Period = 0
        });

        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        provider.Load();
        await Task.Delay(50);

        Assert.True(provider.IsPolling, "Provider has begun polling");
    }

    [Fact]
    public void KubernetesSecretProvider_AddsToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"Secret","apiVersion":"v1","metadata":{"name":"testsecret","namespace":"default","selfLink":"/api/v1/namespaces/default/secrets/testsecret","uid":"04a256d5-5480-4e6a-ab1a-81b1df2b1f15","resourceVersion":"724153","creationTimestamp":"2020-04-17T14:32:42Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"kind\":\"Secret\",\"metadata\":{\"annotations\":{},\"name\":\"testsecret\",\"namespace\":\"default\"},\"type\":\"Opaque\"}\n"}},"data":{"testKey":"dGVzdFZhbHVl"},"type":"Opaque"}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testsecret", new ReloadSettings());
        var provider = new KubernetesSecretProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("testKey", out string? testValue));
        Assert.Equal("testValue", testValue);
    }

    [Fact]
    public void KubernetesSecretProvider_SeesDoubleUnderscore()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"Secret","apiVersion":"v1","metadata":{"name":"testsecret","namespace":"default","selfLink":"/api/v1/namespaces/default/secrets/testsecret","uid":"04a256d5-5480-4e6a-ab1a-81b1df2b1f15","resourceVersion":"724153","creationTimestamp":"2020-04-17T14:32:42Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"kind\":\"Secret\",\"metadata\":{\"annotations\":{},\"name\":\"testsecret\",\"namespace\":\"default\"},\"type\":\"Opaque\"}\n"}},"data":{"several__layers__deep__testKey":"dGVzdFZhbHVl"},"type":"Opaque"}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testsecret", new ReloadSettings());
        var provider = new KubernetesSecretProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("several:layers:deep:testKey", out string? testValue));
        Assert.Equal("testValue", testValue);
    }

    [Fact]
    public async Task KubernetesSecretProvider_ReloadsDictionaryOnInterval()
    {
        bool foundKey = false;
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"Secret","apiVersion":"v1","metadata":{"name":"testsecret","namespace":"default","selfLink":"/api/v1/namespaces/default/secrets/testsecret","uid":"04a256d5-5480-4e6a-ab1a-81b1df2b1f15","resourceVersion":"724153","creationTimestamp":"2020-04-17T14:32:42Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"kind\":\"Secret\",\"metadata\":{\"annotations\":{},\"name\":\"testsecret\",\"namespace\":\"default\"},\"type\":\"Opaque\"}\n"}},"data":{"testKey":"dGVzdFZhbHVl"},"type":"Opaque"}
            """));

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"Secret","apiVersion":"v1","metadata":{"name":"testsecret","namespace":"default","selfLink":"/api/v1/namespaces/default/secrets/testsecret","uid":"04a256d5-5480-4e6a-ab1a-81b1df2b1f15","resourceVersion":"724153","creationTimestamp":"2020-04-17T14:32:42Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"kind\":\"Secret\",\"metadata\":{\"annotations\":{},\"name\":\"testsecret\",\"namespace\":\"default\"},\"type\":\"Opaque\"}\n"}},"data":{"updatedKey":"dGVzdFZhbHVl"},"type":"Opaque"}
            """));

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"Secret","apiVersion":"v1","metadata":{"name":"testsecret","namespace":"default","selfLink":"/api/v1/namespaces/default/secrets/testsecret","uid":"04a256d5-5480-4e6a-ab1a-81b1df2b1f15","resourceVersion":"724153","creationTimestamp":"2020-04-17T14:32:42Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"kind\":\"Secret\",\"metadata\":{\"annotations\":{},\"name\":\"testsecret\",\"namespace\":\"default\"},\"type\":\"Opaque\"}\n"}},"data":{"updatedAgain":"dGVzdFZhbHVl"},"type":"Opaque"}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testsecret", new ReloadSettings
        {
            Period = 1,
            Secrets = true
        });

        var provider = new KubernetesSecretProvider(client, settings, new CancellationTokenSource(20000).Token);

        provider.Load();

        Assert.True(provider.TryGet("testKey", out string? testValue), "TryGet testKey");
        Assert.Equal("testValue", testValue);

        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("updatedKey", out testValue);

            if (foundKey)
            {
                Assert.Equal("testValue", testValue);
            }
        }

        foundKey = false;

        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("updatedAgain", out testValue);

            if (foundKey)
            {
                Assert.Equal("testValue", testValue);
            }
        }

        Assert.False(provider.TryGet("testKey", out _), "TryGet testKey after update");
        Assert.False(provider.TryGet("updatedKey", out _), "TryGet updatedKey after update");
    }
}
