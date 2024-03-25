// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.Discovery;
using Xunit;

namespace Steeltoe.Common.Http.Test.Discovery;

public sealed class DiscoveryHttpClientHandlerHelperTest
{
    [Fact]
    public async Task LookupServiceAsync_NonDefaultPort_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var handler = new DiscoveryHttpClientHandlerHelper(client, NullLoggerFactory.Instance);
        var uri = new Uri("https://foo:8080/test");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task LookupServiceAsync_DoesNotFindService_ReturnsOriginalURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient();
        var handler = new DiscoveryHttpClientHandlerHelper(client, NullLoggerFactory.Instance);
        var uri = new Uri("https://foo/test");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(uri, result);
    }

    [Fact]
    public async Task LookupServiceAsync_FindsService_ReturnsURI()
    {
        IDiscoveryClient client = new TestDiscoveryClient(new TestServiceInstance(new Uri("https://foundit:5555")));
        var handler = new DiscoveryHttpClientHandlerHelper(client, NullLoggerFactory.Instance);
        var uri = new Uri("https://foo/test/bar/foo?test=1&test2=2");

        Uri result = await handler.LookupServiceAsync(uri, CancellationToken.None);
        Assert.Equal(new Uri("https://foundit:5555/test/bar/foo?test=1&test2=2"), result);
    }

    private sealed class TestServiceInstance : IServiceInstance
    {
        public string ServiceId => throw new NotImplementedException();
        public string Host => throw new NotImplementedException();
        public int Port => throw new NotImplementedException();
        public bool IsSecure => throw new NotImplementedException();
        public Uri Uri { get; }
        public IReadOnlyDictionary<string, string?> Metadata => throw new NotImplementedException();

        public TestServiceInstance(Uri uri)
        {
            Uri = uri;
        }
    }

    private sealed class TestDiscoveryClient : IDiscoveryClient
    {
        private readonly IServiceInstance? _instance;

        public string Description => throw new NotImplementedException();

        public TestDiscoveryClient(IServiceInstance? instance = null)
        {
            _instance = instance;
        }

        public Task<IList<string>> GetServiceIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
        {
            IList<IServiceInstance> instances = [];

            if (_instance != null)
            {
                instances.Add(_instance);
            }

            return Task.FromResult(instances);
        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
