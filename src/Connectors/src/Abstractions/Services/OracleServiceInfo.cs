// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class OracleServiceInfo : UriServiceInfo
{
    public const string ORACLE_SCHEME = "oracle";

    public OracleServiceInfo(string id, string url)
        : base(id, url)
    {
    }
}
