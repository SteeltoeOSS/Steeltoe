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

namespace Steeltoe.Connector
{
    internal static class ConnectionTypeLocator
    {
        internal static Type GetConnectionInfoType(string input)
        {
            return true switch
            {
                bool _ when input.Equals("cosmosdb", StringComparison.InvariantCultureIgnoreCase) => typeof(CosmosDbConnectionInfo),
                bool _ when input.Equals("cosmosdb-readonly", StringComparison.InvariantCultureIgnoreCase) => typeof(CosmosDbReadOnlyConnectionInfo),
                bool _ when input.Equals("mongodb", StringComparison.InvariantCultureIgnoreCase) => typeof(MongoDbConnectionInfo),
                bool _ when input.Equals("mysql", StringComparison.InvariantCultureIgnoreCase) => typeof(MySqlConnectionInfo),
                bool _ when input.Equals("oracle", StringComparison.InvariantCultureIgnoreCase) => typeof(OracleConnectionInfo),
                bool _ when input.Equals("oracledb", StringComparison.InvariantCultureIgnoreCase) => typeof(OracleConnectionInfo),
                bool _ when input.Equals("postgres", StringComparison.InvariantCultureIgnoreCase) => typeof(PostgresConnectionInfo),
                bool _ when input.Equals("postgresql", StringComparison.InvariantCultureIgnoreCase) => typeof(PostgresConnectionInfo),
                bool _ when input.Equals("rabbitmq", StringComparison.InvariantCultureIgnoreCase) => typeof(RabbitMQConnectionInfo),
                bool _ when input.Equals("redis", StringComparison.InvariantCultureIgnoreCase) => typeof(RedisConnectionInfo),
                bool _ when input.Equals("sqlserver", StringComparison.InvariantCultureIgnoreCase) => typeof(SqlServerConnectionInfo),
                _ => throw new ConnectorException($"Could not find a matching IConnectionInfo for {input}"),
            };
        }

        internal static Type GetConnectionInfoType(IServiceInfo input)
        {
            return input switch
            {
                CosmosDbServiceInfo _ => typeof(CosmosDbConnectionInfo),
                MongoDbServiceInfo _ => typeof(MongoDbConnectionInfo),
                MySqlServiceInfo _ => typeof(MySqlConnectionInfo),
                OracleServiceInfo _ => typeof(OracleConnectionInfo),
                PostgresServiceInfo _ => typeof(PostgresConnectionInfo),
                RabbitMQServiceInfo _ => typeof(RabbitMQConnectionInfo),
                RedisServiceInfo _ => typeof(RedisConnectionInfo),
                SqlServerServiceInfo _ => typeof(SqlServerConnectionInfo),
                _ => throw new ConnectorException($"Could not find a matching IConnectionInfo for {input.GetType().Name}"),
            };
        }
    }
}
