// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services
{
    public class DB2ServiceInfoFactory : RelationalServiceInfoFactory
    {
        private static readonly Tags _db2tags = new (new[] { "sqldb", "dashDB", "db2" });

        public DB2ServiceInfoFactory()
            : base(_db2tags, DB2ServiceInfo.DB2_SCHEME)
        {
        }

        public override IServiceInfo Create(string id, string url)
        {
            return new DB2ServiceInfo(id, url);
        }
    }
}
