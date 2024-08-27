// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Connectors.EntityFrameworkCore;

/// <summary>
/// Applies code-first database migrations for the specified Entity Framework Core <see cref="DbContext" />.
/// </summary>
/// <example><![CDATA[
/// dotnet run RunTask=migrate
/// ]]>
/// </example>
/// <typeparam name="TDbContext">
/// The <see cref="DbContext" /> to run migrations from.
/// </typeparam>
public sealed class MigrateDbContextTask<TDbContext> : IApplicationTask
    where TDbContext : DbContext
{
    public const string Name = "migrate";

    private readonly TDbContext _dbContext;
    private readonly ILogger _logger;

    public MigrateDbContextTask(TDbContext dbContext, ILogger<MigrateDbContextTask<TDbContext>> logger)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(logger);

        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        bool isNewDatabase = false;
        IList<string> migrations = Array.Empty<string>();

        try
        {
            migrations = (await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        }
        catch
        {
            // This might not be the true source of the error, but we'll catch the real cause as part of the MigrateAsync call.
            isNewDatabase = true;
        }

        _logger.LogInformation("Starting database migration...");
        await _dbContext.Database.MigrateAsync(cancellationToken);

        if (isNewDatabase)
        {
            migrations = (await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();
        }

        if (migrations.Count > 0)
        {
            string migrationNames = string.Join(", ", migrations);
            _logger.LogInformation("The following migrations have been successfully applied: {MigrationNames}.", migrationNames);
        }
        else
        {
            _logger.LogInformation("Database is already up to date.");
        }
    }
}
