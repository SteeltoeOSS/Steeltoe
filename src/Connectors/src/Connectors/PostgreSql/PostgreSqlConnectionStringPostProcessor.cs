// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Connectors.PostgreSql.RuntimeTypeAccess;

namespace Steeltoe.Connectors.PostgreSql;

internal sealed class PostgreSqlConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    private readonly PostgreSqlPackageResolver _packageResolver;

    protected override string BindingType => "postgresql";

    public PostgreSqlConnectionStringPostProcessor(PostgreSqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);
        _packageResolver = packageResolver;
    }

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        // The connection string parameters are documented at:
        // https://www.npgsql.org/doc/connection-string-parameters.html

        var npgsqlConnectionStringBuilderShim = NpgsqlConnectionStringBuilderShim.CreateInstance(_packageResolver);
        return new DbConnectionStringBuilderWrapper(npgsqlConnectionStringBuilderShim.Instance);
    }
}
