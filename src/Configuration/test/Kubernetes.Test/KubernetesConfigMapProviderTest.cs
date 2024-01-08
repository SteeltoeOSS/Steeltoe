// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using k8s;
using k8s.Autorest;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.Test;

public sealed class KubernetesConfigMapProviderTest
{
    [Fact]
    public void KubernetesConfigMapProvider_ThrowsOn403()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.Forbidden);
        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        var exception = Assert.Throws<HttpOperationException>(provider.Load);

        Assert.Equal(HttpStatusCode.Forbidden, exception.Response.StatusCode);
    }

    [Fact]
    public async Task KubernetesConfigMapProvider_ContinuesOn404()
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
    public void KubernetesConfigMapProvider_AddsToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"TestKey":"TestValue"}}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("TestKey", out string? testValue));
        Assert.Equal("TestValue", testValue);
    }

    [Fact]
    public void KubernetesConfigMapProvider_SeesDoubleUnderscore()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"several__layers__deep__TestKey":"TestValue"}}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("several:layers:deep:TestKey", out string? testValue));
        Assert.Equal("TestValue", testValue);
    }

    [Fact]
    public async Task KubernetesConfigMapProvider_ReloadsDictionaryOnInterval()
    {
        bool foundKey = false;
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"TestKey":"TestValue"}}
            """));

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"TestKey2":"UpdatedValue"}}
            """));

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"TestKey3":"UpdatedAgain"}}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings
        {
            Period = 1,
            ConfigMaps = true
        });

        var provider = new KubernetesConfigMapProvider(client, settings, new CancellationTokenSource(20000).Token);

        provider.Load();

        Assert.True(provider.TryGet("TestKey", out string? testValue), "TryGet TestKey");
        Assert.Equal("TestValue", testValue);

        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("TestKey2", out string? testValue2);

            if (foundKey)
            {
                Assert.Equal("UpdatedValue", testValue2);
            }
        }

        foundKey = false;

        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("TestKey3", out string? testValue3);

            if (foundKey)
            {
                Assert.Equal("UpdatedAgain", testValue3);
            }
        }

        Assert.False(provider.TryGet("TestKey", out _), "TryGet TestKey after update");
        Assert.False(provider.TryGet("TestKey2", out _), "TryGet TestKey2 after update");
    }

    [Fact]
    public void KubernetesConfigMapProvider_AddsJsonFileToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"appsettings.json": "{\"Test0\": \"Value0\",\"Test1\": [{\"Test2\": \"Value1\"}]}"}}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("Test0", out string? testValue));
        Assert.Equal("Value0", testValue);

        Assert.True(provider.TryGet("Test1:0:Test2", out string? testValue2));
        Assert.Equal("Value1", testValue2);
    }

    [Fact]
    public void KubernetesConfigMapProvider_AddsEnvSpecificJsonFileToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();

        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(new StringContent("""
            {"kind":"ConfigMap","apiVersion":"v1","metadata":{"name":"testconfigmap","namespace":"default","selfLink":"/api/v1/namespaces/default/configmaps/testconfigmap","uid":"8582b94c-f4fa-47fa-bacc-47019223775c","resourceVersion":"1320622","creationTimestamp":"2020-04-15T18:33:49Z","annotations":{"kubectl.kubernetes.io/last-applied-configuration":"{\"apiVersion\":\"v1\",\"data\":{\"ConfigMapName\":\"testconfigmap\"},\"kind\":\"ConfigMap\",\"metadata\":{\"annotations\":{},\"name\":\"kubernetes1\",\"namespace\":\"default\"}}"}},"data":{"appsettings.demo.json": "{\"Test0\": \"Value0\",\"Test1\": [{\"Test2\": \"Value1\"}]}"}}
            """));

        using var delegatingHandler = new HttpClientDelegatingHandler(mockHttpMessageHandler.ToHttpClient());

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        }, delegatingHandler);

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);

        provider.Load();

        Assert.True(provider.TryGet("Test0", out string? testValue));
        Assert.Equal("Value0", testValue);

        Assert.True(provider.TryGet("Test1:0:Test2", out string? testValue2));
        Assert.Equal("Value1", testValue2);
    }

    [Fact]
    public void KubernetesProviderGetsNewLoggerFactory()
    {
        // arrange
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        });

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);
        ILoggerFactory? originalLoggerFactory = settings.LoggerFactory;
        var newFactory = new LoggerFactory();
        provider.ProvideRuntimeReplacements(newFactory);

        Assert.Equal(newFactory, settings.LoggerFactory);
        Assert.NotEqual(originalLoggerFactory, settings.LoggerFactory);
    }

    [Fact]
    public void KubernetesProviderGetsNewLogger()
    {
        // arrange
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration
        {
            Host = "http://localhost"
        });

        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings, CancellationToken.None);
        settings.LoggerFactory ??= new LoggerFactory();
        ILogger<KubernetesConfigMapProviderTest> firstLogger = settings.LoggerFactory.CreateLogger<KubernetesConfigMapProviderTest>();

        provider.ProvideRuntimeReplacements(new LoggerFactory());
        ILogger<KubernetesConfigMapProviderTest> secondLogger = settings.LoggerFactory.CreateLogger<KubernetesConfigMapProviderTest>();

        Assert.NotEqual(firstLogger.GetHashCode(), secondLogger.GetHashCode());
    }
}
