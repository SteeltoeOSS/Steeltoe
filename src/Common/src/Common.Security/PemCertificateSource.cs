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
    public class PemCertificateSource : ICertificateSource
    {
        private string _certFilePath;
        private string _keyFilePath;

        public PemCertificateSource(string certFilePath, string keyFilePath)
        {
            _certFilePath = Path.GetFullPath(certFilePath);
            _keyFilePath = Path.GetFullPath(keyFilePath);
        }

        public Type OptionsConfigurer => typeof(PemConfigureCertificateOptions);

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var certSource = new FileSource("certificate")
            {
                FileProvider = null,
                Path = Path.GetFileName(_certFilePath),
                Optional = false,
                ReloadOnChange = true,
                ReloadDelay = 1000
            };

            var keySource = new FileSource("privateKey")
            {
                FileProvider = null,
                Path = Path.GetFileName(_keyFilePath),
                Optional = false,
                ReloadOnChange = true,
                ReloadDelay = 1000,
            };

            var certProvider = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(_certFilePath))
                .Add(certSource)
                .Build();

            var keyProvider = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(_keyFilePath))
                .Add(keySource)
                .Build();

            return new PemCertificateProvider(certProvider, keyProvider);
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    internal class FileSource : FileConfigurationSource
    {
        internal string BasePath { get; set; }

        internal string Key { get; }

        public FileSource(string key)
            : base()
        {
            Key = key;
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new FileProvider(this);
        }
    }

    internal class FileProvider : FileConfigurationProvider
    {
        public FileProvider(FileConfigurationSource source)
            : base(source)
        {
        }

        public override void Load(Stream stream)
        {
            var source = Source as FileSource;
            string key = source.Key;
            using (var reader = new StreamReader(stream))
            {
                string value = reader.ReadToEnd();
                Data[key] = value;
            }
        }
    }
#pragma warning restore SA1402 // File may only contain a single class
}
