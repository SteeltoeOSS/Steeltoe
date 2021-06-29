// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services
{
    public class MongoDbServiceInfoFactory : ServiceInfoFactory
    {
        public MongoDbServiceInfoFactory()
            : base(new Tags("mongodb"), MongoDbServiceInfo.MONGODB_SCHEME)
        {
            // add the uri property used by the Microsoft Azure Service Broker with CosmosDb
            UriKeys.Add("cosmosdb_connection_string");
        }

        public override IServiceInfo Create(Service binding)
        {
            var uri = GetUriFromCredentials(binding.Credentials);
            return new MongoDbServiceInfo(binding.Name, uri);
        }
    }
}
