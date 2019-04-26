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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Test;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MySql.Test
{
    public class MySqlProviderServiceCollectionExtensionsTest
    {
        public MySqlProviderServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddMySqlConnection_ThrowsIfServiceCollectionNull()
        {
            // Arrange
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddMySqlConnection_ThrowsIfConfigurationNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddMySqlConnection_ThrowsIfServiceNameNull()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddMySqlConnection_NoVCAPs_AddsMySqlConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config);

           var service = services.BuildServiceProvider().GetService<IDbConnection>();
           Assert.NotNull(service);
        }

        [Fact]
        public void AddMySqlConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = new ConfigurationBuilder().Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddMySqlConnection_MultipleMySqlServices_ThrowsConnectorException()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVCAP);

            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            var ex = Assert.Throws<ConnectorException>(() => MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddMySqlConnection_WithVCAPs_AddsMySqlConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVCAP);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act and Assert
            MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.Contains("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", connString);
            Assert.Contains("3306", connString);
            Assert.Contains("192.168.0.90", connString);
            Assert.Contains("Dd6O1BPXUHdrmzbP", connString);
            Assert.Contains("7E1LxXnlH2hhlPVt", connString);
        }

        [Fact]
        public void AddMySqlConnection_WithAzureBrokerVCAPs_AddsMySqlConnection()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerAzureVCAP);
            var appsettings = new Dictionary<string, string>()
            {
                ["mysql:client:urlEncodedCredentials"] = "true"
            };
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            builder.AddInMemoryCollection(appsettings);
            var config = builder.Build();

            // Act and Assert
            MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config);

            var service = services.BuildServiceProvider().GetService<IDbConnection>();
            Assert.NotNull(service);
            var connString = service.ConnectionString;
            Assert.Contains("ub6oyk1kkh", connString);         // database
            Assert.Contains("3306", connString);                                            // port
            Assert.Contains("451200b4-c29d-4346-9a0a-70bc109bb6e9.mysql.database.azure.com", connString);                                    // host
            Assert.Contains("wj7tsxai7i@451200b4-c29d-4346-9a0a-70bc109bb6e9", connString); // user
            Assert.Contains("10PUO82Uhqk8F2ii", connString);                                // password
        }

        [Fact]
        public void AddMySqlConnection_AddsRelationalHealthContributor()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            // Act
            MySqlProviderServiceCollectionExtensions.AddMySqlConnection(services, config);
            var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalHealthContributor;

            // Assert
            Assert.NotNull(healthContributor);
        }
    }
}
