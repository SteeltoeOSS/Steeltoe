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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Steeltoe.Common.Security
{
    public class Pkcs12ConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
    {
        private readonly IConfiguration _config;

        public Pkcs12ConfigureCertificateOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }

        public void Configure(string name, CertificateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Name = name;

            var certContents = _config["certificate"];

            if (string.IsNullOrEmpty(certContents))
            {
                return;
            }

            options.Certificate = new X509Certificate2(Encoding.UTF8.GetBytes(certContents));
        }

        public void Configure(CertificateOptions options)
        {
            Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
        }
    }
}
