// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

internal sealed class SqlConnectionStringBuilderShim : Shim
{
    public override DbConnectionStringBuilder Instance => (DbConnectionStringBuilder)base.Instance;

    private SqlConnectionStringBuilderShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static SqlConnectionStringBuilderShim CreateInstance(SqlServerPackageResolver packageResolver)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.SqlConnectionStringBuilderClass.CreateInstance(null);
        return new SqlConnectionStringBuilderShim(instanceAccessor);
    }
}
