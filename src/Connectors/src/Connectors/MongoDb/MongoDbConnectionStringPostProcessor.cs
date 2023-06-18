// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Connectors.MongoDb;

internal sealed class MongoDbConnectionStringPostProcessor : ConnectionStringPostProcessor
{
    protected override string BindingType => "mongodb";

    protected override IConnectionStringBuilder CreateConnectionStringBuilder()
    {
        return new MongoDbConnectionStringBuilder();
    }

    protected override bool IsPartOfConnectionString(string secretName)
    {
        return !string.Equals(secretName, "database", StringComparison.OrdinalIgnoreCase);
    }
}
