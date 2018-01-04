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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Security.DataProtection.CredHub
{
    internal static class CertificateHelpers
    {
        internal static X509Certificate2 GetX509FromBytes(byte[] clientCertificate, byte[] clientKey)
        {
            var cert = new X509Certificate2(clientCertificate);
            object obj;

            using (var reader = new StreamReader(new MemoryStream(clientKey)))
            {
                obj = new PemReader(reader).ReadObject();
                if (obj is AsymmetricCipherKeyPair cipherKey)
                {
                    obj = cipherKey.Private;
                }
            }

            var rsaKeyParams = (RsaPrivateCrtKeyParameters)obj;
#if NET461
            var pk = RSA.Create();
            pk.ImportParameters(DotNetUtilities.ToRSAParameters(rsaKeyParams));
            cert.PrivateKey = pk;
            return cert;
#else
            var rsaKey = RSA.Create(DotNetUtilities.ToRSAParameters(rsaKeyParams));
            return cert.CopyWithPrivateKey(rsaKey);
#endif
        }
    }
}
