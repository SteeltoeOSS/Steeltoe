// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;

namespace Steeltoe.CloudFoundry.Connector.Services
{
    public class MongoDbServiceInfoFactory : ServiceInfoFactory
    {
        public MongoDbServiceInfoFactory()
            : base(new Tags("mongodb"), MongoDbServiceInfo.MONGODB_SCHEME)
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            string uri = GetUriFromCredentials(binding.Credentials);
            return new MongoDbServiceInfo(binding.Name, uri);
        }
    }
}
