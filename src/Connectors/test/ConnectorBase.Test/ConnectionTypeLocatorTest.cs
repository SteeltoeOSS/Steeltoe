// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.CosmosDb;
using Steeltoe.Connector.MongoDb;
using Steeltoe.Connector.MySql;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.PostgreSql;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Redis;
using Steeltoe.Connector.Services;
using Steeltoe.Connector.SqlServer;
using System;
using Xunit;

namespace Steeltoe.Connector.Test
{
    public class ConnectionTypeLocatorTest
    {
        [Theory]
        [InlineData("cosMosdb", typeof(CosmosDbConnectionInfo))]
        [InlineData("cosmosdb-readonly", typeof(CosmosDbReadOnlyConnectionInfo))]
        [InlineData("mongodb", typeof(MongoDbConnectionInfo))]
        [InlineData("mYsql", typeof(MySqlConnectionInfo))]
        [InlineData("oracle", typeof(OracleConnectionInfo))]
        [InlineData("oracledb", typeof(OracleConnectionInfo))]
        [InlineData("postgres", typeof(PostgresConnectionInfo))]
        [InlineData("postgresql", typeof(PostgresConnectionInfo))]
        [InlineData("rabbitmq", typeof(RabbitMQConnectionInfo))]
        [InlineData("redis", typeof(RedisConnectionInfo))]
        [InlineData("sqlserver", typeof(SqlServerConnectionInfo))]
        public void ConnectionTypeLocatorFindsType(string value, Type type)
        {
            Assert.IsType(ConnectionTypeLocator.GetConnectionInfoType(value).GetType(), type);
        }

        [Theory]
        [InlineData("squirrelQL")]
        [InlineData("anyqueue")]
        public void ConnectionTypeLocatorThrowsOnUnknown(string value)
        {
            var exception = Assert.Throws<ConnectorException>(() => ConnectionTypeLocator.GetConnectionInfoType(value));
            Assert.Contains(value, exception.Message);
        }

        [Fact]
        public void ConnectionTypeLocatorFindsTypeFromServiceInfo()
        {
            var cosmosInfo = new CosmosDbServiceInfo("id");
            var mongoInfo = new MongoDbServiceInfo("id", "mongodb://host");
            var mysqlInfo = new MySqlServiceInfo("id", "mysql://host");
            var oracleInfo = new OracleServiceInfo("id", "oracle://host");
            var postgresInfo = new PostgresServiceInfo("id", "postgres://host");
            var rabbitMqInfo = new RabbitMQServiceInfo("id", "rabbitmq://host");
            var redisInfo = new RedisServiceInfo("id", "redis://host");
            var sqlInfo = new SqlServerServiceInfo("id", "sqlserver://host");

            Assert.Equal(typeof(CosmosDbConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(cosmosInfo));
            Assert.Equal(typeof(MongoDbConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(mongoInfo));
            Assert.Equal(typeof(MySqlConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(mysqlInfo));
            Assert.Equal(typeof(OracleConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(oracleInfo));
            Assert.Equal(typeof(PostgresConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(postgresInfo));
            Assert.Equal(typeof(RabbitMQConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(rabbitMqInfo));
            Assert.Equal(typeof(RedisConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(redisInfo));
            Assert.Equal(typeof(SqlServerConnectionInfo), ConnectionTypeLocator.GetConnectionInfoType(sqlInfo));
        }

        [Fact]
        public void ConnectionTypeLocatorFromInfoThrowsOnUnknown()
        {
            var exception = Assert.Throws<ConnectorException>(() => ConnectionTypeLocator.GetConnectionInfoType(new DB2ServiceInfo("id", "http://idk")));
            Assert.Contains("DB2ServiceInfo", exception.Message);
        }
    }
}
