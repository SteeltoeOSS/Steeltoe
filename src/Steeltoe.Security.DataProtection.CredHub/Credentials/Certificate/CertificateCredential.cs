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

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CertificateCredential : ICredentialValue
    {
        /// <summary>
        /// Certificate of the Certificate Authority
        /// </summary>
        [JsonProperty("ca")]
        public string CertificateAuthority { get; set; }

        /// <summary>
        /// String representation of the certificate
        /// </summary>
        public string Certificate { get; set; }

        /// <summary>
        /// Private key for the certificate
        /// </summary>
        [JsonProperty("private_key")]
        public string PrivateKey { get; set; }
    }
}
