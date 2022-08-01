// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Kubernetes;
using System.Net;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test;

public class KubernetesConfigMapProviderTest
{
    [Fact]
    public void KubernetesConfigMapProvider_ThrowsOnNulls()
    {
        var client = new k8s.Kubernetes(KubernetesClientConfiguration.BuildDefaultConfig());
        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());

        var ex1 = Assert.Throws<ArgumentNullException>(() => new KubernetesConfigMapProvider(null, settings));
        var ex2 = Assert.Throws<ArgumentNullException>(() => new KubernetesConfigMapProvider(client, null));

        Assert.Equal("kubernetes", ex1.ParamName);
        Assert.Equal("settings", ex2.ParamName);
    }

    [Fact]
    public void KubernetesConfigMapProvider_ThrowsOn403()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.Forbidden);

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);

        var ex = Assert.Throws<HttpOperationException>(() => provider.Load());

        Assert.Equal(HttpStatusCode.Forbidden, ex.Response.StatusCode);
    }

    [Fact]
    public async Task KubernetesConfigMapProvider_ContinuesOn404()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings { ConfigMaps = true, Period = 0 });
        var provider = new KubernetesConfigMapProvider(client, settings);

        provider.Load();
        await Task.Delay(50);

        Assert.True(provider.Polling, "Provider has begun polling");
    }

    [Fact]
    public void KubernetesConfigMapProvider_AddsToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n"));

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);

        provider.Load();

        Assert.True(provider.TryGet("TestKey", out var testValue));
        Assert.Equal("TestValue", testValue);
    }

    [Fact]
    public void KubernetesConfigMapProvider_SeesDoubleUnderscore()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"several__layers__deep__TestKey\":\"TestValue\"}}\n"));

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);

        provider.Load();

        Assert.True(provider.TryGet("several:layers:deep:TestKey", out var testValue));
        Assert.Equal("TestValue", testValue);
    }

    [Fact]
    public async Task KubernetesConfigMapProvider_ReloadsDictionaryOnInterval()
    {
        var foundKey = false;
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n"));
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey2\":\"UpdatedValue\"}}\n"));
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey3\":\"UpdatedAgain\"}}\n"));

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings { Period = 1, ConfigMaps = true });
        var provider = new KubernetesConfigMapProvider(client, settings, new CancellationTokenSource(20000).Token);

        provider.Load();

        Assert.True(provider.TryGet("TestKey", out var testValue), "TryGet TestKey");
        Assert.Equal("TestValue", testValue);

        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("TestKey2", out var testValue2);
            if (foundKey)
            {
                Assert.Equal("UpdatedValue", testValue2);
            }
        }

        foundKey = false;
        while (!foundKey)
        {
            await Task.Delay(100);
            foundKey = provider.TryGet("TestKey3", out var testValue3);
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
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{" +
                                       "\"appsettings.json\": \"{\n  \\\"Test0\\\": \\\"Value0\\\",\n  \\\"Test1\\\": [\n    {\n      \\\"Test2\\\": \\\"Value1\\\"\n    }\n  ]\n}\"" +
                                       "}\n}\n"));

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);

        provider.Load();

        Assert.True(provider.TryGet("Test0", out var testValue));
        Assert.Equal("Value0", testValue);

        Assert.True(provider.TryGet("Test1:0:Test2", out var testValue2));
        Assert.Equal("Value1", testValue2);
    }

    [Fact]
    public void KubernetesConfigMapProvider_AddsEnvSpecificJsonFileToDictionaryOnSuccess()
    {
        var mockHttpMessageHandler = new MockHttpMessageHandler();
        mockHttpMessageHandler
            .Expect(HttpMethod.Get, "*")
            .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{" +
                                       "\"appsettings.demo.json\": \"{\n  \\\"Test0\\\": \\\"Value0\\\",\n  \\\"Test1\\\": [\n    {\n      \\\"Test2\\\": \\\"Value1\\\"\n    }\n  ]\n}\"" +
                                       "}\n}\n"));

        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);

        provider.Load();

        Assert.True(provider.TryGet("Test0", out var testValue));
        Assert.Equal("Value0", testValue);

        Assert.True(provider.TryGet("Test1:0:Test2", out var testValue2));
        Assert.Equal("Value1", testValue2);
    }

    [Fact]
    public void KubernetesProviderGetsNewLoggerFactory()
    {
        // arrange
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, new HttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);
        var originalLoggerFactory = settings.LoggerFactory;
        var newFactory = new LoggerFactory();
        provider.ProvideRuntimeReplacements(newFactory);

        Assert.Equal(newFactory, settings.LoggerFactory);
        Assert.NotEqual(originalLoggerFactory, settings.LoggerFactory);
    }

    [Fact]
    public void KubernetesProviderGetsNewLogger()
    {
        // arrange
        using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, new HttpClient());
        var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
        var provider = new KubernetesConfigMapProvider(client, settings);
        settings.LoggerFactory ??= new LoggerFactory();
        var firstLogger = settings.LoggerFactory.CreateLogger<KubernetesConfigMapProviderTest>();

        provider.ProvideRuntimeReplacements(new LoggerFactory());
        var secondLogger = settings.LoggerFactory.CreateLogger<KubernetesConfigMapProviderTest>();

        Assert.NotEqual(firstLogger.GetHashCode(), secondLogger.GetHashCode());
    }
}
