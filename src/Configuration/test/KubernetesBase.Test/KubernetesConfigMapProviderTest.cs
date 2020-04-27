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
using System;
using System.Net;
using System.Net.Http;
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
            var settings = new KubernetesConfigSourceSettings("default", "test");

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

            var client = new k8s.Kubernetes(new KubernetesClientConfiguration { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test");
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            var ex = Assert.Throws<HttpOperationException>(() => provider.Load());

            // assert
            Assert.Equal(HttpStatusCode.Forbidden, ex.Response.StatusCode);
        }

        [Fact(Skip = "Server response not mocked yet")]
        public void KubernetesConfigMapProvider_AddsToDictionaryOnSuccess()
        {
            // arrange
            var mockHttpMessageHandler = new MockHttpMessageHandler();

            var client = new k8s.Kubernetes(new KubernetesClientConfiguration() { Host = "http://localhost" }, httpClient: mockHttpMessageHandler.ToHttpClient());
            var settings = new KubernetesConfigSourceSettings("default", "test");
            var provider = new KubernetesConfigMapProvider(client, settings);

            // act
            Assert.Throws<HttpOperationException>(() => provider.Load());

            // assert
            Assert.True(provider.TryGet("testKey", out var testValue));
            Assert.Equal("testValue", testValue);
        }
    }
}
