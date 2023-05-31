// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Connectors.MySql.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MySql;

internal sealed class MySqlConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    private readonly MySqlPackageResolver _packageResolver;

    protected override string BindingType => "mysql";

    public MySqlConnectionStringPostProcessor(MySqlPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);
        _packageResolver = packageResolver;
    }

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        // The connection string parameters are documented at:
        // - MySqlConnector: https://mysqlconnector.net/connection-options/
        // - Oracle: https://dev.mysql.com/doc/refman/8.0/en/connecting-using-uri-or-key-value-pairs.html#connection-parameters-base

        var mySqlConnectionStringBuilderShim = MySqlConnectionStringBuilderShim.CreateInstance(_packageResolver);
        return new DbConnectionStringBuilderWrapper(mySqlConnectionStringBuilderShim.Instance);
    }
}
