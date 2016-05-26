//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Options;
using System;
using Xunit;

namespace SteelToe.Discovery.Client.Test
{
    public class DiscoveryClientFactoryTest : AbstractBaseTest
    {
        public DiscoveryClientFactoryTest() : base()
        {
            DiscoveryClientFactory._discoveryClient = null;
        }
        [Fact]
        public void CreateClient_NullOptions_ReturnsUnknownClient()
        {
            IDiscoveryClient result = DiscoveryClientFactory.CreateClient(null) as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.Description);
        }

        [Fact]
        public void CreateClient_UnknownClientType_ReturnsUnknownClient()
        {
            var result = DiscoveryClientFactory.CreateClient(new DiscoveryOptions()) as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.Description);
        }

        [Fact]
        public void CreateClient_ClientTypeEureka_CreatesClient()
        {
            DiscoveryOptions options = new DiscoveryOptions()
            {
                ClientType = DiscoveryClientType.EUREKA,
                ClientOptions = new EurekaClientOptions()
                {
                    ShouldFetchRegistry = false,
                    ShouldRegisterWithEureka = false

                }
            };
            var client = DiscoveryClientFactory.CreateClient(options);
            Assert.NotNull(client);
            Assert.IsType(typeof(EurekaDiscoveryClient), client);
        }

        [Fact]
        public void CreateDiscoveryClient_NullIServiceProvider_ReturnsNull()
        {
            var result = DiscoveryClientFactory.CreateDiscoveryClient(null);
            Assert.Null(result);
        }

        [Fact]
        public void CreateDiscoveryClient_CreatesClients()
        {
            DiscoveryOptions options = new DiscoveryOptions()
            {
                ClientType = DiscoveryClientType.EUREKA,
                ClientOptions = new EurekaClientOptions()
                {
                    ShouldFetchRegistry = false,
                    ShouldRegisterWithEureka = false

                }
            };
            IServiceProvider provider = new MyServiceProvier(new TestOptions(options));
            var result = DiscoveryClientFactory.CreateDiscoveryClient(provider);
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateDiscoveryClient_MissingOptions_ReturnsNull()
        {
            IServiceProvider provider = new MyServiceProvier(null);
            IDiscoveryClient result = DiscoveryClientFactory.CreateDiscoveryClient(provider) as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.Description);
        }

    }

    class MyServiceProvier : IServiceProvider
    {
        private TestOptions _options;
        public MyServiceProvier(TestOptions options)
        {
            _options = options;
        }
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IOptions<DiscoveryOptions>))
            {
                return _options;
            }
            return null;
        }
    }
    class TestOptions : IOptions<DiscoveryOptions>
    {
        private DiscoveryOptions _options;
        public TestOptions(DiscoveryOptions options = null)
        {
            _options = options;
        }
        public DiscoveryOptions Value
        {
            get
            {
                return _options;
            }
        }
    }
}
