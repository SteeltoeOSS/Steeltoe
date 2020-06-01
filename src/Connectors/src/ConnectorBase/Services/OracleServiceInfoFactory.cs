// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class OracleServiceInfoFactory : RelationalServiceInfoFactory
    {
        public OracleServiceInfoFactory()
            : base(new Tags("oracle"), OracleServiceInfo.ORACLE_SCHEME)
        {
        }

        public override IServiceInfo Create(string id, string url)
        {
            return new OracleServiceInfo(id, url);
        }
    }
}
