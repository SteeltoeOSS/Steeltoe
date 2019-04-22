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

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    public class RedisCacheConfigurationTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new RedisCacheConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = @"
{
   'redis': {
        'client': {
            'host': 'localhost',
            'port': 1234,
            'password': 'password',
            'instanceid': 'instanceid',
            'allowAdmin': true,
            'clientName': 'foobar',
            'connectRetry': 100,
            'connectTimeout': 100,
            'abortOnConnectFail': true,
            'keepAlive': 100,
            'resolveDns': true,
            'serviceName': 'foobar',
            'ssl': true,
            'sslHost': 'foobar',
            'writeBuffer': 100,
            'tieBreaker': 'foobar'
        }
   }
}";

            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);
            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var sconfig = new RedisCacheConnectorOptions(config);
            Assert.Equal("localhost", sconfig.Host);
            Assert.Equal(1234, sconfig.Port);
            Assert.Equal("password", sconfig.Password);
            Assert.Equal("instanceid", sconfig.InstanceId);

            Assert.True( sconfig.AllowAdmin);
            Assert.Equal("foobar", sconfig.ClientName);
            Assert.Equal(100, sconfig.ConnectRetry);
            Assert.Equal(100, sconfig.ConnectTimeout);
            Assert.True( sconfig.AbortOnConnectFail);
            Assert.Equal(100, sconfig.KeepAlive);
            Assert.True( sconfig.ResolveDns);
            Assert.Equal("foobar", sconfig.ServiceName);
            Assert.True( sconfig.Ssl);
            Assert.Equal("foobar", sconfig.SslHost);
            Assert.Equal("foobar", sconfig.TieBreaker);
            Assert.Equal(100, sconfig.WriteBuffer);


            Assert.Null( sconfig.ConnectionString);


    }
    }
}

