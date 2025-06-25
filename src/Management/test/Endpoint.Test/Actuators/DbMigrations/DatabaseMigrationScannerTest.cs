// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.Actuators.DbMigrations;

public sealed class DatabaseMigrationScannerTest
{
    [Fact]
    public void Reflection_loaded_methods_are_available()
    {
        DatabaseMigrationScanner.GetDatabaseMethod.Should().NotBeNull();
        DatabaseMigrationScanner.GetMigrationsMethod.Should().NotBeNull();
        DatabaseMigrationScanner.DbContextType.Should().NotBeNull();
        DatabaseMigrationScanner.GetAppliedMigrationsMethod.Should().NotBeNull();
        DatabaseMigrationScanner.GetPendingMigrationsMethod.Should().NotBeNull();
    }

    [Fact]
    public void Throws_when_no_database_provider_configured()
    {
        var scanner = new DatabaseMigrationScanner();
        var dbContext = new TestDbContext();

        Action action = () => scanner.GetMigrations(dbContext);

        action.Should().ThrowExactly<TargetInvocationException>().WithInnerExceptionExactly<InvalidOperationException>()
            .WithMessage("*No database provider has been configured for this DbContext*");
    }
}
