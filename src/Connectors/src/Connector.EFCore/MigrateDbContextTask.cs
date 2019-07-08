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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.CloudFoundry.Connector.EFCore
{
    /// <summary>
    /// Applies code first migrations for the specified Entity Framework DB Context
    /// This task name is "migrate"
    /// </summary>
    /// <example>
    /// dotnet run runtask=migrate
    /// </example>
    /// <typeparam name="T">The DBContext which to migrate</typeparam>
    public class MigrateDbContextTask<T> : IApplicationTask
        where T : DbContext
    {
        private readonly T _db;
        private readonly ILogger _logger;

        public MigrateDbContextTask(T db, ILogger<MigrateDbContextTask<T>> logger)
        {
            _db = db;
            _logger = logger;
        }

        public string Name => "migrate";

        public void Run()
        {
            var isNewDb = false;
            var migrations = new List<string>();
            try
            {
                migrations = _db.Database.GetPendingMigrations().ToList();
            }
            catch
            {
                isNewDb = true; // might not be true source of the error, but we'll catch real cause as part of Migrate call
            }

            _logger.LogInformation("Starting database migration...");
            _db.Database.Migrate();
            if (isNewDb)
            {
                migrations = _db.Database.GetAppliedMigrations().ToList();
            }

            if (migrations.Any())
            {
                _logger.LogInformation("The following migrations have been successfully applied:");
                foreach (var migration in migrations)
                {
                    _logger.LogInformation(migration);
                }
            }
            else
            {
                _logger.LogInformation("Database is already up to date");
            }
        }
    }
}