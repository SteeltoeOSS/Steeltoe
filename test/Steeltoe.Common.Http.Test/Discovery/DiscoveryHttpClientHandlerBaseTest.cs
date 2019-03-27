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

using Steeltoe.Common.Discovery;
using System;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    public class DiscoveryHttpClientHandlerBaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfClientNull()
        {
            // Arrange
            IDiscoveryClient client = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryHttpClientHandlerBase(client));
            Assert.Contains(nameof(client), ex.Message);
        }

        [Fact]
        public void LookupService_NonDefaultPort_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo:8080/test");

            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public void LookupService_DoesntFindService_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo/test");

            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public void LookupService_FindsService_ReturnsURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("http://foundit:5555")));
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo/test/bar/foo?test=1&test2=2");

            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(new Uri("http://foundit:5555/test/bar/foo?test=1&test2=2"), result);
        }

        [Fact]
        public async void LookupServiceAsync_NonDefaultPort_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo:8080/test");

            // Act and Assert
            var result = await handler.LookupServiceAsync(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public async void LookupServiceAsync_DoesntFindService_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo/test");

            // Act and Assert
            var result = await handler.LookupServiceAsync(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public async void LookupServiceAsync_FindsService_ReturnsURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("http://foundit:5555")));
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("http://foo/test/bar/foo?test=1&test2=2");

            // Act and Assert
            var result = await handler.LookupServiceAsync(uri);
            Assert.Equal(new Uri("http://foundit:5555/test/bar/foo?test=1&test2=2"), result);
        }
    }
}