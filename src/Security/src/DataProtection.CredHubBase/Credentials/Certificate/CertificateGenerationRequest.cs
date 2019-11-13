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
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CertificateGenerationRequest : CredHubGenerateRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateGenerationRequest"/> class.
        /// Use to request a new Certificate
        /// </summary>
        /// <param name="credentialName">Name of the credential</param>
        /// <param name="parameters">Variables for certificate generation</param>
        /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
        public CertificateGenerationRequest(string credentialName, CertificateGenerationParameters parameters, OverwiteMode overwriteMode = OverwiteMode.converge)
        {
            var subjects = new List<string> { parameters.CommonName, parameters.Organization, parameters.OrganizationUnit, parameters.Locality, parameters.State, parameters.Country };
            if (!AtLeastOneProvided(subjects))
            {
                throw new ArgumentException("At least one subject value, such as common name or organization must be defined to generate the certificate");
            }

            if (string.IsNullOrEmpty(parameters.CertificateAuthority) && !parameters.IsCertificateAuthority && !parameters.SelfSign)
            {
                throw new ArgumentException("At least one signing parameter must be specified");
            }

            Name = credentialName;
            Type = CredentialType.Certificate;
            Parameters = parameters;
            Mode = overwriteMode;
        }

        private bool AtLeastOneProvided(List<string> parms)
        {
            foreach (var s in parms)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    return true;
                }
            }

            return false;
        }
    }
}