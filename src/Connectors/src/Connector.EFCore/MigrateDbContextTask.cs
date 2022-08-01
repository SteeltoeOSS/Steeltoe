// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Connector.EFCore;

/// <summary>
/// Applies code first migrations for the specified Entity Framework DB Context
/// This task name is "migrate".
/// </summary>
/// <example>
/// dotnet run runtask=migrate.
/// </example>
/// <typeparam name="T">The DBContext which to migrate.</typeparam>
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
