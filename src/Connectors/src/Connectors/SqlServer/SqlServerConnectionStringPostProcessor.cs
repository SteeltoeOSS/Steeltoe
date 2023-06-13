// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

internal sealed class SqlServerConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    private readonly SqlServerPackageResolver _packageResolver;

    protected override string BindingType => "sqlserver";

    public SqlServerConnectionStringPostProcessor(SqlServerPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);

        _packageResolver = packageResolver;
    }

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        // The connection string parameters are documented at:
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlconnection.connectionstring#remarks

        var sqlConnectionStringBuilderShim = SqlConnectionStringBuilderShim.CreateInstance(_packageResolver);
        return new DbConnectionStringBuilderWrapper(sqlConnectionStringBuilderShim.Instance);
    }
}
