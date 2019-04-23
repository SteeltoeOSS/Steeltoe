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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.IO;
using System.Text;
using MS = System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Security
{
    public class PemConfigureCertificateOptions : IConfigureNamedOptions<CertificateOptions>
    {
        private IConfiguration _config;
        private ILogger _logger;

        public PemConfigureCertificateOptions(IConfiguration config, ILogger logger = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _logger = logger;
        }

        public void Configure(string name, CertificateOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Name = name;

            var pemCert = _config["certificate"];
            var pemKey = _config["privateKey"];

            if (string.IsNullOrEmpty(pemCert) || string.IsNullOrEmpty(pemKey))
            {
                return;
            }

            var certBytes = Encoding.Default.GetBytes(pemCert);
            var keyBytes = Encoding.Default.GetBytes(pemKey);

            X509Certificate cert = ReadCertificate(certBytes);
            AsymmetricCipherKeyPair keys = ReadKeys(keyBytes);

            var pfxBytes = CreatePfxContainer(cert, keys);
            options.Certificate = new MS.X509Certificate2(pfxBytes);
        }

        public void Configure(CertificateOptions options)
        {
            Configure(Options.DefaultName, options);
        }

        internal byte[] CreatePfxContainer(X509Certificate cert, AsymmetricCipherKeyPair keys)
        {
            var certEntry = new X509CertificateEntry(cert);

            var pkcs12Store = new Pkcs12StoreBuilder()
                .SetUseDerEncoding(true)
                .Build();
            var keyEntry = new AsymmetricKeyEntry(keys.Private);
            pkcs12Store.SetKeyEntry("ServerInstance", keyEntry, new X509CertificateEntry[] { certEntry });

            using (MemoryStream stream = new MemoryStream())
            {
                pkcs12Store.Save(stream, null, new SecureRandom());
                var bytes = stream.ToArray();
                return Pkcs12Utilities.ConvertToDefiniteLength(bytes);
            }
        }

        internal AsymmetricCipherKeyPair ReadKeys(byte[] keyBytes)
        {
            try
            {
                using (var reader = new StreamReader(new MemoryStream(keyBytes)))
                {
                    return new PemReader(reader).ReadObject() as AsymmetricCipherKeyPair;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Unable to read PEM encoded keys");
            }

            return null;
        }

        internal X509Certificate ReadCertificate(byte[] certBytes)
        {
            try
            {
                using (var reader = new StreamReader(new MemoryStream(certBytes)))
                {
                    return new PemReader(reader).ReadObject() as X509Certificate;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Unable to read PEM encoded certificate");
            }

            return null;
        }
    }
}
