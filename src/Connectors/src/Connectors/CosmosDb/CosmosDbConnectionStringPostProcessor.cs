// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;

namespace Steeltoe.Connectors.CosmosDb;

internal sealed class CosmosDbConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "cosmosdb";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        var dbConnectionStringBuilder = new DbConnectionStringBuilder();
        return new DbConnectionStringBuilderWrapper(dbConnectionStringBuilder);
    }

    protected override bool IsPartOfConnectionString(string secretName)
    {
        return string.Equals(secretName, "accountEndpoint", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(secretName, "accountKey", StringComparison.OrdinalIgnoreCase);
    }
}
