// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using k8s;
using Microsoft.Rest;
using Moq;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Kubernetes;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class KubernetesConfigMapProviderTest
    {
        [Fact]
        public void KubernetesConfigMapProvider_ThrowsOnNulls()
        {
            // arrange
            var client = new Mock<k8s.Kubernetes>();
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());

            // act
            var ex1 = Assert.Throws<ArgumentNullException>(() => new KubernetesConfigMapProvider(null, settings));
            var ex2 = Assert.Throws<ArgumentNullException>(() => new KubernetesConfigMapProvider(client.Object, null));

            // assert
            Assert.Equal("kubernetes", ex1.ParamName);
            Assert.Equal("settings", ex2.ParamName);
        }

        [Fact]
        public void KubernetesConfigMapProvider_ThrowsOn403()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.Forbidden);

            using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            var ex = Assert.Throws<HttpOperationException>(() => provider.Load());

            // assert
            Assert.Equal(HttpStatusCode.Forbidden, ex.Response.StatusCode);
        }

        [Fact]
        public async Task KubernetesConfigMapProvider_ContinuesOn404()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);

            using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings() { ConfigMaps = true, Period = 0 });
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            provider.Load();
            await Task.Delay(50);

            // assert
            Assert.True(provider.Polling, "Provider has begun polling");
        }

        [Fact]
        public void KubernetesConfigMapProvider_AddsToDictionaryOnSuccess()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n"));

            using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings());
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            provider.Load();

            // assert
            Assert.True(provider.TryGet("TestKey", out var testValue));
            Assert.Equal("TestValue", testValue);
        }

        [Fact]
        public async Task KubernetesConfigMapProvider_ReloadsDictionaryOnInterval()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"TestValue\"}}\n"));
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"UpdatedValue\"}}\n"));
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"ConfigMap\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testconfigmap\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/configmaps/testconfigmap\",\"uid\":\"8582b94c-f4fa-47fa-bacc-47019223775c\",\"resourceVersion\":\"1320622\",\"creationTimestamp\":\"2020-04-15T18:33:49Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"ConfigMapName\\\":\\\"testconfigmap\\\"},\\\"kind\\\":\\\"ConfigMap\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"kubernetes1\\\",\\\"namespace\\\":\\\"default\\\"}}\\n\"}},\"data\":{\"TestKey\":\"UpdatedAgain\"}}\n"));

            using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "testconfigmap", new ReloadSettings() { Period = 1, ConfigMaps = true });
            var provider = new KubernetesConfigMapProvider(client, settings, new CancellationTokenSource(20000).Token);

            // act
            provider.Load();

            // assert
            Assert.True(provider.TryGet("TestKey", out var testValue), "TryGet TestKey x1");
            Assert.Equal("TestValue", testValue);
            await Task.Delay(1100);
            Assert.True(provider.TryGet("TestKey", out var testValue2), "TryGet TestKey x2");
            Assert.Equal("UpdatedValue", testValue2);
            await Task.Delay(1100);
            Assert.True(provider.TryGet("TestKey", out var testValue3), "TryGet TestKey x3");
            Assert.Equal("UpdatedAgain", testValue3);
        }
    }
}
