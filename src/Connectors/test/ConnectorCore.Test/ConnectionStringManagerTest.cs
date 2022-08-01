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
using Steeltoe.Connector.Services;
using Steeltoe.Connector.SqlServer;
using Steeltoe.Connector.SqlServer.Test;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.Test;

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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);
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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.TwoServerVcapEdb);
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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVcap);

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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.TwoServerVcap);

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
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MongoDbTestHelpers.DoubleBindingEnterpriseVcap);

        var cm = new ConnectionStringManager(new ConfigurationBuilder().AddCloudFoundry().Build());
        var connInfo = cm.Get<MongoDbConnectionInfo>("steeltoe");

        Assert.NotNull(connInfo);
        Assert.Equal("MongoDb-steeltoe", connInfo.Name);
    }

    [Theory]
    [InlineData("cosMosdb")]
    [InlineData("cosmosdb-readonly")]
    [InlineData("mongodb")]
    [InlineData("mYsql")]
    [InlineData("oracle")]
    [InlineData("postgres")]
    [InlineData("rabbitmq")]
    [InlineData("redis")]
    [InlineData("sqlserver")]
    public void ConnectionInfoTypeFoundByName(string value)
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        Assert.StartsWith(value, manager.GetByTypeName(value).Name, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData("squirrelQL")]
    [InlineData("anyqueue")]
    public void ConnectionTypeLocatorThrowsOnUnknown(string value)
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetByTypeName(value));
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
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());

        Assert.StartsWith("CosmosDb", manager.GetFromServiceInfo(cosmosInfo).Name);
        Assert.StartsWith("MongoDb", manager.GetFromServiceInfo(mongoInfo).Name);
        Assert.StartsWith("MySql", manager.GetFromServiceInfo(mysqlInfo).Name);
        Assert.StartsWith("Oracle", manager.GetFromServiceInfo(oracleInfo).Name);
        Assert.StartsWith("Postgres", manager.GetFromServiceInfo(postgresInfo).Name);
        Assert.StartsWith("RabbitMQ", manager.GetFromServiceInfo(rabbitMqInfo).Name);
        Assert.StartsWith("Redis", manager.GetFromServiceInfo(redisInfo).Name);
        Assert.StartsWith("SqlServer", manager.GetFromServiceInfo(sqlInfo).Name);
    }

    [Fact]
    public void ConnectionTypeLocatorFromInfoThrowsOnUnknown()
    {
        var manager = new ConnectionStringManager(new ConfigurationBuilder().Build());
        var exception = Assert.Throws<ConnectorException>(() => manager.GetFromServiceInfo(new Db2ServiceInfo("id", "http://idk")));
        Assert.Contains("Db2ServiceInfo", exception.Message);
    }
}
