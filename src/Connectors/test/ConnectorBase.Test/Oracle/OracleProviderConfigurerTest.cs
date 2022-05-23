// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Oracle.Test
{
    public class OracleProviderConfigurerTest
    {
        [Fact]
        public void UpdateConfiguration_WithNullOracleServiceInfo_ReturnsExpected()
        {
            var configurer = new OracleProviderConfigurer();
            var config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                ServiceName = "orcl"
            };
            configurer.UpdateConfiguration(null, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal(1234, config.Port);
            Assert.Equal("username", config.Username);
            Assert.Equal("password", config.Password);
            Assert.Equal("orcl", config.ServiceName);
            Assert.Null(config.ConnectionString);
        }

        [Fact]
        public void UpdateConfiguration_WithOracleServiceInfo_ReturnsExpected()
        {
            var configurer = new OracleProviderConfigurer();
            var config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                ServiceName = "orcl"
            };
            var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");

            configurer.UpdateConfiguration(si, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal(1521, config.Port);
            Assert.Equal("user", config.Username);
            Assert.Equal("pwd", config.Password);
            Assert.Equal("orclpdb1", config.ServiceName);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            var config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                ServiceName = "orcl",
                ConnectionTimeout = 10
            };

            var configurer = new OracleProviderConfigurer();
            var opts = configurer.Configure(null, config);
            var connectionString =
                $"User Id={config.Username};Password={config.Password};Data Source={config.Server}:{config.Port}/{config.ServiceName};Connection Timeout={config.ConnectionTimeout}";
            Assert.StartsWith(connectionString, opts);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            var config = new OracleProviderConnectorOptions()
            {
                Server = "localhost",
                Port = 1234,
                Username = "username",
                Password = "password",
                ServiceName = "orcl"
            };

            var configurer = new OracleProviderConfigurer();
            var si = new OracleServiceInfo("MyId", "oracle://user:pwd@localhost:1521/orclpdb1");

            _ = configurer.Configure(si, config);

            Assert.Equal("localhost", config.Server);
            Assert.Equal(1521, config.Port);
            Assert.Equal("user", config.Username);
            Assert.Equal("pwd", config.Password);
            Assert.Equal("orclpdb1", config.ServiceName);
        }
    }
}
