// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

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

            var cert = ReadCertificate(certBytes);
            var keys = ReadKeys(keyBytes);

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

            using var stream = new MemoryStream();
            pkcs12Store.Save(stream, null, new SecureRandom());
            var bytes = stream.ToArray();
            return Pkcs12Utilities.ConvertToDefiniteLength(bytes);
        }

        internal AsymmetricCipherKeyPair ReadKeys(byte[] keyBytes)
        {
            try
            {
                using var reader = new StreamReader(new MemoryStream(keyBytes));
                return new PemReader(reader).ReadObject() as AsymmetricCipherKeyPair;
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
                using var reader = new StreamReader(new MemoryStream(certBytes));
                return new PemReader(reader).ReadObject() as X509Certificate;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Unable to read PEM encoded certificate");
            }

            return null;
        }
    }
}
