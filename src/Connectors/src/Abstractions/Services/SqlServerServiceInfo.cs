// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class SqlServerServiceInfo : UriServiceInfo
{
    public static readonly string[] SqlServerScheme =
    {
        "sqlserver",
        "jdbc:sqlserver",
        "mssql"
    };

    public SqlServerServiceInfo(string id, string url)
        : base(id, url)
    {
    }

    public SqlServerServiceInfo(string id, string url, string username, string password)
        : base(id, url, username, password)
    {
    }
}
