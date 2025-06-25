// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Reflection;
using Steeltoe.Management.Endpoint.Actuators.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.Actuators.DbMigrations;

internal sealed class FakeDatabaseMigrationScanner : IDatabaseMigrationScanner
{
    public Assembly AssemblyToScan => typeof(TestDbContext).Assembly;
    public bool ThrowOnGetPendingMigrations { get; set; }

    public IEnumerable<string> GetPendingMigrations(object context)
    {
        if (ThrowOnGetPendingMigrations)
        {
            throw new TestDbException("database doesn't exist");
        }

        return ["pending"];
    }

    public IEnumerable<string> GetAppliedMigrations(object context)
    {
        return ["applied"];
    }

    public IEnumerable<string> GetMigrations(object context)
    {
        return ["migration"];
    }

    private sealed class TestDbException(string message)
        : DbException(message);
}
