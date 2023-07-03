// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors.Services;

public class PostgreSqlServiceInfo : UriServiceInfo
{
    public const string PostgreSqlScheme = "postgres";
    public const string PostgreSqlJdbcScheme = "postgresql";

    public PostgreSqlServiceInfo(string id, string url)
        : base(id, url)
    {
    }
}
