// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql.DynamicTypeAccess;

internal sealed class MySqlConnectionShim : Shim, IDisposable, IAsyncDisposable
{
    public override DbConnection Instance => (DbConnection)base.Instance;

    private MySqlConnectionShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static MySqlConnectionShim CreateInstance(MySqlPackageResolver packageResolver, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.MySqlConnectionClass.CreateInstance(connectionString);
        return new MySqlConnectionShim(instanceAccessor);
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
