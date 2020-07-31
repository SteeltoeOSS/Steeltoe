// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class PostgresServiceInfoFactory : RelationalServiceInfoFactory
    {
        private static readonly string[] _postschemes = new string[] { PostgresServiceInfo.POSTGRES_SCHEME, PostgresServiceInfo.POSTGRES_JDBC_SCHEME };

        public PostgresServiceInfoFactory()
            : base(new Tags("postgresql"), _postschemes)
        {
        }

        public override IServiceInfo Create(string id, string url)
        {
            return new PostgresServiceInfo(id, url);
        }
    }
}
