// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.PostgreSql.DynamicTypeAccess;

internal sealed class NpgsqlConnectionStringBuilderShim : Shim
{
    public override DbConnectionStringBuilder Instance => (DbConnectionStringBuilder)base.Instance;

    private NpgsqlConnectionStringBuilderShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static NpgsqlConnectionStringBuilderShim CreateInstance(PostgreSqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.NpgsqlConnectionStringBuilderClass.CreateInstance(null);
        return new NpgsqlConnectionStringBuilderShim(instanceAccessor);
    }
}
