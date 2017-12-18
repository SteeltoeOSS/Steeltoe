// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CertificateGenerationParameters : KeyParameters
    {
        /// <summary>
        /// Common name of generated credential value
        /// </summary>
        [JsonProperty("common_name")]
        public string CommonName { get; set; }

        /// <summary>
        /// Alternative names of generated credential value
        /// </summary>
        [JsonProperty("alternative_names")]
        public List<string> AlternativeNames { get; set; }

        /// <summary>
        /// Organization of generated credential value
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Organization Unit of generated credential value
        /// </summary>
        [JsonProperty("organization_unit")]
        public string OrganizationUnit { get; set; }

        /// <summary>
        /// Locality/city of generated credential value
        /// </summary>
        public string Locality { get; set; }

        /// <summary>
        /// State/province of generated credential value
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Country of generated credential value
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Key usage values
        /// </summary>
        [JsonProperty("key_usage")]
        public List<KeyUsage> KeyUsage { get; set; }

        /// <summary>
        /// Extended key usage values
        /// </summary>
        [JsonProperty("extended_key_usage")]
        public List<ExtendedKeyUsage> ExtendedKeyUsage { get; set; }

        /// <summary>
        /// Duration in days of generated credential value
        /// </summary>
        public int Duration { get; set; } = 365;

        /// <summary>
        /// Name of certificate authority to sign of generated credential value
        /// </summary>
        [JsonProperty("ca")]
        public string CertificateAuthority { get; set; }

        /// <summary>
        ///  Whether to generate credential value as a certificate authority
        /// </summary>
        [JsonProperty("is_ca")]
        public bool IsCertificateAuthority { get; set; } = false;

        /// <summary>
        /// Whether to self-sign generated credential value
        /// </summary>
        [JsonProperty("self_sign")]
        public bool SelfSign { get; set; } = false;
    }
}
