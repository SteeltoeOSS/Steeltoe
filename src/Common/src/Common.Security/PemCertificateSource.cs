// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Steeltoe.Common.Security
{
    public class PemCertificateSource : ICertificateSource
    {
        private readonly string _certFilePath;
        private readonly string _keyFilePath;

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

    internal class FileSource : FileConfigurationSource
    {
        internal string BasePath { get; set; }

        internal string Key { get; }

        public FileSource(string key)
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
            var key = source.Key;
            using var reader = new StreamReader(stream);
            var value = reader.ReadToEnd();
            Data[key] = value;
        }
    }
}
