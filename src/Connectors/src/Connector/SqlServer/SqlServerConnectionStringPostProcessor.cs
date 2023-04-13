// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace Steeltoe.Connector.SqlServer;

internal sealed class SqlServerConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "sqlserver";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        var dbConnectionStringBuilder = (DbConnectionStringBuilder)Activator.CreateInstance(SqlServerTypeLocator.SqlConnectionStringBuilderType);
        return new DbConnectionStringBuilderWrapper(dbConnectionStringBuilder);
    }
}
