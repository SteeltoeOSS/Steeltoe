// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.MongoDb;
using Steeltoe.Connector.MongoDb.Test;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.MySql.Test;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.PostgreSql.Test;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Redis;
using Steeltoe.Connector.Redis.Test;
using Steeltoe.Connector.SqlServer;
using Steeltoe.Connector.SqlServer.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.Test
{
    public class ConnectionStringManagerTest
    {
        [Fact]
        public void MysqlConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<MySqlConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("Server=localhost;Port=3306;", connInfo.ConnectionString);
            Assert.Equal("MySql", connInfo.Name);
        }

        [Fact]
        public void MysqlConnectionInfoByName()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVCAP);
            var config = new ConfigurationBuilder().AddCloudFoundry().Build();

            var cm = new ConnectionStringManager(config);
            var connInfo = cm.Get<MySqlConnectionInfo>("spring-cloud-broker-db");

            Assert.NotNull(connInfo);
            Assert.Equal("MySql-spring-cloud-broker-db", connInfo.Name);
        }

        [Fact]
        public void PostgresConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<PostgresConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("Host=localhost;Port=5432;Timeout=15;Command Timeout=30;", connInfo.ConnectionString);
            Assert.Equal("Postgres", connInfo.Name);
        }

        [Fact]
        public void PostgresConnectionInfoByName()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.TwoServerVCAP_EDB);
            var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
            var connInfo = cm.Get<PostgresConnectionInfo>("myPostgres");

            Assert.NotNull(connInfo);
            Assert.Equal("Postgres-myPostgres", connInfo.Name);
        }

        [Fact]
        public void SqlServerConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<SqlServerConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("Data Source=localhost,1433;", connInfo.ConnectionString);
            Assert.Equal("SqlServer", connInfo.Name);
        }

        [Fact]
        public void SqlServerConnectionInfo_ByName()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVCAP);

            var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
            var connInfo = cm.Get<SqlServerConnectionInfo>("mySqlServerService");

            Assert.NotNull(connInfo);
            Assert.Equal("SqlServer-mySqlServerService", connInfo.Name);
        }

        [Fact]
        public void RedisConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<RedisConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("localhost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connInfo.ConnectionString);
            Assert.Equal("Redis", connInfo.Name);
        }

        [Fact]
        public void RedisConnectionInfoByName()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.TwoServerVCAP);

            var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
            var connInfo = cm.Get<RedisConnectionInfo>("myRedisService1");

            Assert.NotNull(connInfo);
            Assert.Equal("Redis-myRedisService1", connInfo.Name);
        }

        [Fact]
        public void RabbitMQConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<RabbitMQConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("amqp://127.0.0.1:5672/", connInfo.ConnectionString);
            Assert.Equal("RabbitMQ", connInfo.Name);
        }

        [Fact]
        public void MongoDbConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<MongoDbConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("mongodb://localhost:27017", connInfo.ConnectionString);
            Assert.Equal("MongoDb", connInfo.Name);
        }

        [Fact]
        public void MongoDbConnectionInfoByName()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.DoubleBinding_Enterprise_VCAP);

            var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
            var connInfo = cm.Get<MongoDbConnectionInfo>("steeltoe");

            Assert.NotNull(connInfo);
            Assert.Equal("MongoDb-steeltoe", connInfo.Name);
        }
    }
}
