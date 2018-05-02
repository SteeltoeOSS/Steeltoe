// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Data;

namespace Steeltoe.CloudFoundry.ConnectorBase.Relational
{
    public class RelationalHealthContributor : IHealthContributor
    {
        public readonly IDbConnection _connection;
        private readonly ILogger<IDbConnection> _logger;
        private readonly string _dbType;

        public RelationalHealthContributor(IDbConnection connection, ILogger<IDbConnection> logger)
        {
            _connection = connection;
            _logger = logger;
            _dbType = GetDbName(connection);
        }

        public string Id => _dbType;

        public Health Health()
        {
            _logger.LogInformation($"Checking {_dbType} connection health!");
            Health result = new Health();
            result.Details.Add("database", _dbType);
            try
            {
                _connection.Open();
                var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT 1;";
                var qresult = cmd.ExecuteScalar();
                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger.LogInformation($"{_dbType} connection up!");
            }
            catch (Exception e)
            {
                _logger.LogInformation($"{_dbType} connection down!");
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
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
            }

            return result;
        }
    }
}
