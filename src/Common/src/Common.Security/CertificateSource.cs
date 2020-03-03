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
using System;
using System.IO;

namespace Steeltoe.Common.Security
{
    public class CertificateSource : ICertificateSource
    {
        private readonly string _certFilePath;

        public CertificateSource(string certFilePath)
        {
            _certFilePath = Path.GetFullPath(certFilePath);
        }

        public Type OptionsConfigurer => throw new NotImplementedException();

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            if (!File.Exists(_certFilePath))
            {
                throw new InvalidOperationException("Required certificate file not found:" + _certFilePath);
            }

            var certSource = new FileSource("certificate")
            {
                FileProvider = null,
                Path = Path.GetFileName(_certFilePath),
                Optional = false,
                ReloadOnChange = true,
                ReloadDelay = 1000
            };

            var certProvider = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(_certFilePath))
                .Add(certSource)
                .Build();

            return new CertificateProvider(certProvider);
        }
    }
}
