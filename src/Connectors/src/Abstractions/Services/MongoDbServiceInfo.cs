// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class MongoDbServiceInfo : UriServiceInfo
{
    public const string MONGODB_SCHEME = "mongodb";

    public MongoDbServiceInfo(string id, string host, int port, string username, string password, string db)
        : base(id, MONGODB_SCHEME, host, port, username, password, db)
    {
    }

    public MongoDbServiceInfo(string id, string uri)
        : base(id, uri)
    {
    }
}
