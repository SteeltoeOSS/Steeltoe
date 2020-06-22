﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Steeltoe.Discovery;
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
            Uri uri = new Uri("https://foo:8080/test");

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
            Uri uri = new Uri("https://foo/test");

            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public void LookupService_FindsService_ReturnsURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"), result);
        }

        [Fact]
        public async void LookupServiceAsync_NonDefaultPort_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("https://foo:8080/test");

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
            Uri uri = new Uri("https://foo/test");

            // Act and Assert
            var result = await handler.LookupServiceAsync(uri);
            Assert.Equal(uri, result);
        }

        [Fact]
        public async void LookupServiceAsync_FindsService_ReturnsURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
            DiscoveryHttpClientHandlerBase handler = new DiscoveryHttpClientHandlerBase(client);
            Uri uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

            // Act and Assert
            var result = await handler.LookupServiceAsync(uri);
            Assert.Equal(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"), result);
        }
    }
}