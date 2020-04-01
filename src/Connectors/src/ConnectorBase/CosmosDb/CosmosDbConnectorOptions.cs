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

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System.Text;

namespace Steeltoe.Connector.CosmosDb
{
    public class CosmosDbConnectorOptions : AbstractServiceConnectorOptions
    {
        private const string COSMOSDB_CLIENT_SECTION_PREFIX = "cosmosdb:client";

        public CosmosDbConnectorOptions()
        {
        }

        public CosmosDbConnectorOptions(IConfiguration configuration)
            : base(configuration)
        {
            var section = configuration.GetSection(COSMOSDB_CLIENT_SECTION_PREFIX);
            section.Bind(this);
        }

        public string ConnectionString { get; set; }

        public string Host { get; set; }

        public string MasterKey { get; set; }

        public string ReadOnlyKey { get; set; }

        public string DatabaseId { get; set; }

        public string DatabaseLink { get; set; }

        public bool UseReadOnlyCredentials { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ConnectionString) && !Platform.IsCloudFoundry)
            {
                // Connection string was provided and we don't appear to be running on a cloud platform
                return ConnectionString;
            }
            else
            {
                // build a CosmosDB connection string
                var sb = new StringBuilder();

                AddKeyValue(sb, "AccountEndpoint", Host);

                if (UseReadOnlyCredentials)
                {
                    AddKeyValue(sb, "AccountKey", ReadOnlyKey);
                }
                else
                {
                    AddKeyValue(sb, "AccountKey", MasterKey);
                }

                return sb.ToString();
            }
        }
    }
}
