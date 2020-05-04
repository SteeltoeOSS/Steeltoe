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
    public class KubernetesSecretProviderTest
    {
        [Fact]
        public void KubernetesSecretProvider_ThrowsOnNulls()
        {
            // arrange
            var client = new Mock<k8s.Kubernetes>();
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());

            // act
            var ex1 = Assert.Throws<ArgumentNullException>(() => new KubernetesSecretProvider(null, settings));
            var ex2 = Assert.Throws<ArgumentNullException>(() => new KubernetesSecretProvider(client.Object, null));

            // assert
            Assert.Equal("kubernetes", ex1.ParamName);
            Assert.Equal("settings", ex2.ParamName);
        }

        [Fact]
        public void KubernetesSecretProvider_ThrowsOn403()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.Forbidden);

            var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings());
            var provider = new KubernetesSecretProvider(client, settings);

            // act
            var ex = Assert.Throws<HttpOperationException>(() => provider.Load());

            // assert
            Assert.Equal(HttpStatusCode.Forbidden, ex.Response.StatusCode);
        }

        [Fact]
        public async Task KubernetesSecretProvider_ContinuesOn404()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler.Expect(HttpMethod.Get, "*").Respond(HttpStatusCode.NotFound);

            using var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test", new ReloadSettings() { ConfigMaps = true, Period = 0 });
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            provider.Load();
            await Task.Delay(500);

            // assert
            Assert.True(provider.Polling, "Provider has begun polling");
        }

        [Fact]
        public void KubernetesSecretProvider_AddsToDictionaryOnSuccess()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n"));

            var client = new k8s.Kubernetes(new KubernetesClientConfiguration() { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "testsecret", new ReloadSettings());
            var provider = new KubernetesSecretProvider(client, settings);

            // act
            provider.Load();

            // assert
            Assert.True(provider.TryGet("testKey", out var testValue));
            Assert.Equal("testValue", testValue);
        }

        [Fact]
        public async Task KubernetesSecretProvider_ReloadsDictionaryOnInterval()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"testKey\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n"));
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"updatedKey\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n"));
            mockHttpMessageHandler
                .Expect(HttpMethod.Get, "*")
                .Respond(new StringContent("{\"kind\":\"Secret\",\"apiVersion\":\"v1\",\"metadata\":{\"name\":\"testsecret\",\"namespace\":\"default\",\"selfLink\":\"/api/v1/namespaces/default/secrets/testsecret\",\"uid\":\"04a256d5-5480-4e6a-ab1a-81b1df2b1f15\",\"resourceVersion\":\"724153\",\"creationTimestamp\":\"2020-04-17T14:32:42Z\",\"annotations\":{\"kubectl.kubernetes.io/last-applied-configuration\":\"{\\\"apiVersion\\\":\\\"v1\\\",\\\"data\\\":{\\\"testKey\\\":\\\"dGVzdFZhbHVl\\\"},\\\"kind\\\":\\\"Secret\\\",\\\"metadata\\\":{\\\"annotations\\\":{},\\\"name\\\":\\\"testsecret\\\",\\\"namespace\\\":\\\"default\\\"},\\\"type\\\":\\\"Opaque\\\"}\\n\"}},\"data\":{\"updatedAgain\":\"dGVzdFZhbHVl\"},\"type\":\"Opaque\"}\n"));

            var client = new k8s.Kubernetes(new KubernetesClientConfiguration() { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "testsecret", new ReloadSettings() { Period = 1, Secrets = true });
            var provider = new KubernetesSecretProvider(client, settings, new CancellationTokenSource(20000).Token);

            // act
            provider.Load();

            // assert
            Assert.True(provider.TryGet("testKey", out var testValue1), "TryGet testKey");
            Assert.Equal("testValue", testValue1);
            await Task.Delay(1100);
            Assert.True(provider.TryGet("updatedKey", out var testValue2), "TryGet updatedKey");
            Assert.Equal("testValue", testValue2);
            await Task.Delay(1100);
            Assert.True(provider.TryGet("updatedAgain", out var testValue3), "TryGet updatedAgain");
            Assert.Equal("testValue", testValue3);
        }
    }
}
