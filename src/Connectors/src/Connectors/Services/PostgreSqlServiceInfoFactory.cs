// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors.Services;

public class PostgreSqlServiceInfoFactory : RelationalServiceInfoFactory
{
    private static readonly string[] Schemes =
    {
        PostgreSqlServiceInfo.PostgreSqlScheme,
        PostgreSqlServiceInfo.PostgreSqlJdbcScheme
    };

    public PostgreSqlServiceInfoFactory()
        : base(new Tags("postgresql"), Schemes)
    {
    }

    public override IServiceInfo Create(string id, string url)
    {
        return new PostgreSqlServiceInfo(id, url);
    }
}
