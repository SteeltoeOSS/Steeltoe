// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Reflection;
using Steeltoe.Management.Endpoint.DbMigrations;

namespace Steeltoe.Management.Endpoint.Test.DbMigrations;

internal sealed class TestDatabaseMigrationScanner : IDatabaseMigrationScanner
{
    public Assembly AssemblyToScan => typeof(MockDbContext).Assembly;
    public bool ThrowOnGetPendingMigrations { get; set; }

    public IEnumerable<string> GetPendingMigrations(object context)
    {
        if (ThrowOnGetPendingMigrations)
        {
            throw new SomeDbException("database doesn't exist");
        }

        return new[]
        {
            "pending"
        };
    }

    public IEnumerable<string> GetAppliedMigrations(object context)
    {
        return new[]
        {
            "applied"
        };
    }

    public IEnumerable<string> GetMigrations(object context)
    {
        return new[]
        {
            "migration"
        };
    }

    private sealed class SomeDbException : DbException
    {
        public SomeDbException(string message)
            : base(message)
        {
        }
    }
}
