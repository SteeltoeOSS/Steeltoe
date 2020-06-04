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

using System;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    internal class Application
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("managementUrl")]
        public Uri ManagementUrl { get; set; }

        [JsonPropertyName("healthUrl")]
        public Uri HealthUrl { get; set; }

        [JsonPropertyName("serviceUrl")]
        public Uri ServiceUrl { get; set; }

        [JsonPropertyName("metadata")]
        public Metadata Metadata { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class Metadata
#pragma warning restore SA1402 // File may only contain a single type
    {
        [JsonPropertyName("startup")]
        public DateTimeOffset Startup { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class RegistrationResult
#pragma warning restore SA1402 // File may only contain a single type
    {
        public string Id { get; set; }
    }
}
