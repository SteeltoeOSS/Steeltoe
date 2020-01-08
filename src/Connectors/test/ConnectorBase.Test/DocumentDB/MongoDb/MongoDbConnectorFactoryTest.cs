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

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MongoDb.Test
{
    public class MongoDbConnectorFactoryTest
    {
        [Fact]
        public void UpdateConfiguration_WithNullMongoDbServiceInfo_ReturnsExpected()
        {
            var configurer = new MongoDbProviderConfigurer();
            var config = new MongoDbConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                Database = "database"
            };
            configurer.UpdateConfiguration(null, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal(1234, config.Port);
            Assert.Equal("username", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Equal("database", config.Database);
            Assert.Null(config.ConnectionString);
        }

        [Fact]
        public void UpdateConfiguration_WithMongoDbServiceInfo_ReturnsExpected()
        {
            var configurer = new MongoDbProviderConfigurer();
            var config = new MongoDbConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                Database = "database"
            };
            var si = new MongoDbServiceInfo("MyId", "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("192.168.0.90", config.Server);
            Assert.Equal(27017, config.Port);
            Assert.Equal("Dd6O1BPXUHdrmzbP", config.Username);
            Assert.Equal("7E1LxXnlH2hhlPVt", config.Password);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", config.Database);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            var config = new MongoDbConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                Database = "database"
            };

            var configurer = new MongoDbProviderConfigurer();
            var opts = configurer.Configure(null, config);
            Assert.Equal("mongodb://username:password@localhost:1234/database", opts);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            var config = new MongoDbConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                Database = "database"
            };

            var configurer = new MongoDbProviderConfigurer();
            var si = new MongoDbServiceInfo("MyId", "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

            var opts = configurer.Configure(si, config);

            Assert.Equal("mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", opts);
            Assert.DoesNotContain("localhost", opts);
            Assert.DoesNotContain("1234", opts);
            Assert.DoesNotContain("username", opts);
            Assert.DoesNotContain("password", opts);
            Assert.DoesNotContain("database", opts);
        }
    }
}
