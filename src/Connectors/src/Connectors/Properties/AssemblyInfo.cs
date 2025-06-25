// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Aspire;
using Steeltoe.Connectors.CosmosDb;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MySql;
using Steeltoe.Connectors.PostgreSql;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Connectors.Redis;
using Steeltoe.Connectors.SqlServer;

[assembly: ConfigurationSchema("Steeltoe:Client:CosmosDb:Default", typeof(CosmosDbOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:CosmosDb:*", typeof(CosmosDbOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:MongoDb:Default", typeof(MongoDbOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:MongoDb:*", typeof(MongoDbOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:MySql:Default", typeof(MySqlOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:MySql:*", typeof(MySqlOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:PostgreSql:Default", typeof(PostgreSqlOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:PostgreSql:*", typeof(PostgreSqlOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:RabbitMQ:Default", typeof(RabbitMQOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:RabbitMQ:*", typeof(RabbitMQOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:Redis:Default", typeof(RedisOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:Redis:*", typeof(RedisOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:SqlServer:Default", typeof(SqlServerOptions))]
[assembly: ConfigurationSchema("Steeltoe:Client:SqlServer:*", typeof(SqlServerOptions))]
[assembly: LoggingCategories("Steeltoe", "Steeltoe.Connectors")]

[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration")]
[assembly: InternalsVisibleTo("Steeltoe.Bootstrap.AutoConfiguration.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.Test")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Steeltoe.Connectors.EntityFrameworkCore.Test")]
