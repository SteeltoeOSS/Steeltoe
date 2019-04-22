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

namespace Steeltoe.Discovery.Client.Test
{
    public class DiscoveryClientFactoryTest : AbstractBaseTest
    {
        public DiscoveryClientFactoryTest() : base()
        {
        }

        [Fact]
        public void CreateClient_NullOptions_ReturnsUnknownClient()
        {
            DiscoveryClientFactory factory = new DiscoveryClientFactory();
            IDiscoveryClient result = factory.CreateClient() as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.Description);
        }

        [Fact]
        public void CreateClient_UnknownClientType_ReturnsUnknownClient()
        {
            DiscoveryClientFactory factory = new DiscoveryClientFactory(new DiscoveryOptions());
            var result = factory.CreateClient() as IDiscoveryClient;
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
            DiscoveryClientFactory factory = new DiscoveryClientFactory(options);
            var client = factory.CreateClient();
            Assert.NotNull(client);
            Assert.IsType<EurekaDiscoveryClient>(client);
        }

        [Fact]
        public void CreateClient_Calls_ConfigureOptions()
        {
            MyDiscoveryClientFactory factory = new MyDiscoveryClientFactory();
            IServiceProvider provider = new MyServiceProvier();
            IDiscoveryClient result = factory.CreateClient() as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.True(factory.ConfigureOptionsCalled);
        }

        [Fact]
        public void Create_NullIServiceProvider_ReturnsNull()
        {
            DiscoveryClientFactory factory = new DiscoveryClientFactory();
            var result = factory.Create(null);
            Assert.Null(result);
        }

        [Fact]
        public void Create_CreatesClients()
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
            DiscoveryClientFactory factory = new DiscoveryClientFactory(options);
            IServiceProvider provider = new MyServiceProvier();
            var result = factory.Create(provider);
            Assert.NotNull(result);
        }

        [Fact]
        public void Create_MissingOptions_ReturnsUnknown()
        {
            DiscoveryClientFactory factory = new DiscoveryClientFactory();
            IServiceProvider provider = new MyServiceProvier();
            IDiscoveryClient result = factory.Create(provider) as IDiscoveryClient;
            Assert.NotNull(result);
            Assert.Equal("Unknown", result.Description);
        }

    }
    class MyDiscoveryClientFactory : DiscoveryClientFactory
    {
        public bool ConfigureOptionsCalled { get; set; }
        internal protected override void ConfigureOptions()
        {
            this.ConfigureOptionsCalled = true;
        }
    }
    class MyServiceProvier : IServiceProvider
    {

        public MyServiceProvier()
        {
        }
        public object GetService(Type serviceType)
        {
            return null;
        }
    }
}
