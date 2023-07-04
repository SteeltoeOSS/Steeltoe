// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

internal sealed class SqlConnectionShim : Shim, IDisposable, IAsyncDisposable
{
    public override DbConnection Instance => (DbConnection)base.Instance;

    private SqlConnectionShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static SqlConnectionShim CreateInstance(SqlServerPackageResolver packageResolver, string? connectionString)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.SqlConnectionClass.CreateInstance(connectionString);
        return new SqlConnectionShim(instanceAccessor);
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
