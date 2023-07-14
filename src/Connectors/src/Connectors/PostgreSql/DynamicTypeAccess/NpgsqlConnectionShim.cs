// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

internal sealed class NpgsqlConnectionShim : Shim, IDisposable, IAsyncDisposable
{
    public override DbConnection Instance => (DbConnection)base.Instance;

    private NpgsqlConnectionShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static NpgsqlConnectionShim CreateInstance(PostgreSqlPackageResolver packageResolver, string? connectionString)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.NpgsqlConnectionClass.CreateInstance(connectionString);
        return new NpgsqlConnectionShim(instanceAccessor);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return Instance.DisposeAsync();
    }
}
