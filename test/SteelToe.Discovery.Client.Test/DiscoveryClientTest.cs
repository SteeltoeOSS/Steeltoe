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

using Microsoft.Extensions.OptionsModel;
using Xunit;

namespace SteelToe.Discovery.Client.Test
{
    public class DiscoveryClientTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_OptionsNull()
        {
            DiscoveryClient client = new DiscoveryClient(null);
            Assert.Equal("Unknown", client.Description);
            Assert.Null(client.ClientDelegate);
        }

        [Fact]
        public void Constructor_UnknownClientType()
        {
            DiscoveryClient client = new DiscoveryClient(new TestOptions(new DiscoveryOptions()));
            Assert.Equal("Unknown", client.Description);
            Assert.Null(client.ClientDelegate);
        }

        [Fact]
        public void Constructor_ClientTypeEureka_CreatesDelegate()
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
            DiscoveryClient client = new DiscoveryClient(new TestOptions(options));
            Assert.NotNull(client.ClientDelegate);
            Assert.IsType(typeof(EurekaDiscoveryClient), client.ClientDelegate);
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
