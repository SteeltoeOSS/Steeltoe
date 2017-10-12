using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    public class DiscoveryHttpClientHandlerTest
    {
        [Fact]
        public void Constructor_ThrowsIfClientNull()
        {
            // Arrange
            IDiscoveryClient client = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryHttpClientHandler(client));
            Assert.Contains(nameof(client), ex.Message);
        }

        [Fact]
        public void LookupService_NonDefaultPort_ReturnsOriginalURI()
        {
            // Arrange
            IDiscoveryClient client = new TestDiscoveryClient();
            DiscoveryHttpClientHandler handler = new DiscoveryHttpClientHandler(client);
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
            DiscoveryHttpClientHandler handler = new DiscoveryHttpClientHandler(client);
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
            DiscoveryHttpClientHandler handler = new DiscoveryHttpClientHandler(client);
            Uri uri = new Uri("http://foo/test/bar/foo?test=1&test2=2");
            // Act and Assert
            var result = handler.LookupService(uri);
            Assert.Equal(new Uri("http://foundit:5555/test/bar/foo?test=1&test2=2"), result);
        }

    }

    class TestDiscoveryClient : IDiscoveryClient
    {

        private IServiceInstance _instance;
        public TestDiscoveryClient(IServiceInstance instance = null)
        {
            _instance = instance;
        }
        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IList<string> Services
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IList<IServiceInstance> GetInstances(string serviceId)
        {
            if (_instance != null)
            {
                return new List<IServiceInstance>() { _instance };
            }
            return new List<IServiceInstance>();

        }

        public IServiceInstance GetLocalServiceInstance()
        {
            throw new NotImplementedException();
        }

        public Task ShutdownAsync()
        {
            throw new NotImplementedException();
        }
    }

    class TestServiceInstance : IServiceInstance
    {

        private Uri _uri;
        public TestServiceInstance(Uri uri)
        {
            _uri = uri;
        }
        public string Host
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsSecure
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, string> Metadata
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Port
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string ServiceId
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Uri Uri
        {
            get
            {
                return _uri;
            }
        }
    }
}

