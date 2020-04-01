﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
