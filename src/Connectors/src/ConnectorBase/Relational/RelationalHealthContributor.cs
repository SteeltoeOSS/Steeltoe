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
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.Oracle;
using Steeltoe.CloudFoundry.Connector.PostgreSql;
using Steeltoe.CloudFoundry.Connector.SqlServer;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.Connector.Relational
{
    public class RelationalHealthContributor : IHealthContributor
    {
        public static IHealthContributor GetMySqlContributor(IConfiguration configuration, ILogger<RelationalHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var info = configuration.GetSingletonServiceInfo<MySqlServiceInfo>();
            var mySqlConnection = ReflectionHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
            var mySqlConfig = new MySqlProviderConnectorOptions(configuration);
            var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, mySqlConnection);
            var connection = factory.Create(null) as IDbConnection;
            return new RelationalHealthContributor(connection, logger);
        }

        public static IHealthContributor GetPostgreSqlContributor(IConfiguration configuration, ILogger<RelationalHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var info = configuration.GetSingletonServiceInfo<PostgresServiceInfo>();
            var postgresConnection = ReflectionHelpers.FindType(PostgreSqlTypeLocator.Assemblies, PostgreSqlTypeLocator.ConnectionTypeNames);
            var postgresConfig = new PostgresProviderConnectorOptions(configuration);
            var factory = new PostgresProviderConnectorFactory(info, postgresConfig, postgresConnection);
            var connection = factory.Create(null) as IDbConnection;
            return new RelationalHealthContributor(connection, logger);
        }

        public static IHealthContributor GetSqlServerContributor(IConfiguration configuration, ILogger<RelationalHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var info = configuration.GetSingletonServiceInfo<SqlServerServiceInfo>();
            var sqlServerConnection = SqlServerTypeLocator.SqlConnection;
            var sqlServerConfig = new SqlServerProviderConnectorOptions(configuration);
            var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, sqlServerConnection);
            var connection = factory.Create(null) as IDbConnection;
            return new RelationalHealthContributor(connection, logger);
        }

        public static IHealthContributor GetOracleContributor(IConfiguration configuration, ILogger<RelationalHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var info = configuration.GetSingletonServiceInfo<OracleServiceInfo>();
            var oracleConnection = ReflectionHelpers.FindType(OracleTypeLocator.Assemblies, OracleTypeLocator.ConnectionTypeNames);
            var oracleConfig = new OracleProviderConnectorOptions(configuration);
            var factory = new OracleProviderConnectorFactory(info, oracleConfig, oracleConnection);
            var connection = factory.Create(null) as IDbConnection;
            return new RelationalHealthContributor(connection, logger);
        }

        public readonly IDbConnection _connection;
        private readonly ILogger<RelationalHealthContributor> _logger;

        public RelationalHealthContributor(IDbConnection connection, ILogger<RelationalHealthContributor> logger = null)
        {
            _connection = connection;
            _logger = logger;
            Id = GetDbName(connection);
        }

        public string Id { get; }

        public HealthCheckResult Health()
        {
            _logger?.LogTrace("Checking {DbConnection} health", Id);
            var result = new HealthCheckResult();
            result.Details.Add("database", Id);
            try
            {
                _connection.Open();
                var cmd = _connection.CreateCommand();
                cmd.CommandText = (Id.IndexOf("Oracle", StringComparison.OrdinalIgnoreCase) != -1) ? "SELECT 1 FROM dual" : "SELECT 1;";
                var qresult = cmd.ExecuteScalar();
                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger?.LogTrace("{DbConnection} up!", Id);
            }
            catch (Exception e)
            {
                _logger?.LogError("{DbConnection} down! {HealthCheckException}", Id, e.Message);
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
                result.Description = $"{Id} health check failed";
            }
            finally
            {
                _connection.Close();
            }

            return result;
        }

        private string GetDbName(IDbConnection connection)
        {
            var result = "db";
            switch (connection.GetType().Name)
            {
                case "NpgsqlConnection":
                    result = "PostgreSQL";
                    break;
                case "SqlConnection":
                    result = "SqlServer";
                    break;
                case "MySqlConnection":
                    result = "MySQL";
                    break;
                case "OracleConnection":
                    result = "Oracle";
                    break;
            }

            return string.Concat(result, "-", connection.Database);
        }
    }
}
