// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class Db2ServiceInfoFactory : RelationalServiceInfoFactory
{
    private static readonly Tags Db2Tags = new(new[]
    {
        "sqldb",
        "dashDB",
        "db2"
    });

    public Db2ServiceInfoFactory()
        : base(Db2Tags, Db2ServiceInfo.Db2Scheme)
    {
    }

    public override IServiceInfo Create(string id, string url)
    {
        return new Db2ServiceInfo(id, url);
    }
}
