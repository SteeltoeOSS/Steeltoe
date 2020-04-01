// Copyright 2017 the original author or authors.
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
using System.Collections.Generic;

namespace Steeltoe.Connector.Services
{
    public class CosmosDbServiceInfoFactory : ServiceInfoFactory
    {
        public CosmosDbServiceInfoFactory()
            : base(new Tags(new string[] { "azure-cosmosdb", "cosmosdb" }), "cosmosdb") // this URI scheme isn't know to be in use
        {
        }

        public override IServiceInfo Create(Service binding)
        {
            return new CosmosDbServiceInfo(binding.Name)
            {
                Host = GetStringFromCredentials(binding.Credentials, new List<string> { "host_endpoint", "cosmosdb_host_endpoint", "uri" }),
                MasterKey = GetStringFromCredentials(binding.Credentials, new List<string> { "master_key", "cosmosdb_master_key" }),
                ReadOnlyKey = GetStringFromCredentials(binding.Credentials, new List<string> { "readonly_master_key", "readonly_key", "cosmosdb_readonly_master_key" }),
                DatabaseId = GetStringFromCredentials(binding.Credentials, new List<string> { "database_id", "cosmosdb_database_id" }),
                DatabaseLink = GetStringFromCredentials(binding.Credentials, new List<string> { "database_link", "cosmosdb_database_link" })
            };
        }

        public override bool Accept(Service binding)
        {
            return (TagsMatch(binding) || LabelStartsWithTag(binding))
                && IsNotMongoDb(binding);
        }

        private bool IsNotMongoDb(Service binding)
        {
            var connString = GetStringFromCredentials(binding.Credentials, "cosmosdb_connection_string");
            if (!string.IsNullOrEmpty(connString) && connString.StartsWith("mongodb"))
            {
                return false;
            }

            return true;
        }
    }
}
