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

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.PostgreSql;
using Steeltoe.CloudFoundry.Connector.RabbitMQ;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.CloudFoundry.Connector.SqlServer;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test
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
        public void PostgresConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<PostgresConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("Host=localhost;Port=5432;", connInfo.ConnectionString);
            Assert.Equal("Postgres", connInfo.Name);
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
        public void RedisConnectionInfo()
        {
            var cm = new ConnectionStringManager(new ConfigurationBuilder().Build());
            var connInfo = cm.Get<RedisConnectionInfo>();

            Assert.NotNull(connInfo);
            Assert.Equal("localhost:6379,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connInfo.ConnectionString);
            Assert.Equal("Redis", connInfo.Name);
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
    }
}
