// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class PostgresServiceInfo : UriServiceInfo
{
    public const string PostgresScheme = "postgres";
    public const string PostgresJdbcScheme = "postgresql";

    public PostgresServiceInfo(string id, string url)
        : base(id, url)
    {
    }
}
