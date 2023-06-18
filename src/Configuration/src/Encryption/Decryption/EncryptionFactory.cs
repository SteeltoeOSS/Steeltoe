// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Decryption;

internal static class EncryptionFactory
{
    public static ITextDecryptor CreateEncryptor(ConfigServerEncryptionSettings settings)
    {
        ITextDecryptor decryptor = new NoopDecryptor();

        if (settings.EncryptionEnabled)
        {
            if (!string.IsNullOrEmpty(settings.EncryptionKey))
            {
                return new AesTextDecryptor(settings.EncryptionKey);
            }

            var keystoreStream = new FileStream(settings.EncryptionKeyStoreLocation, FileMode.Open, FileAccess.Read);

            return new RsaKeyStoreDecryptor(new KeyProvider(keystoreStream, settings.EncryptionKeyStorePassword), settings.EncryptionKeyStoreAlias,
                settings.EncryptionRsaSalt, settings.EncryptionRsaStrong, settings.EncryptionRsaAlgorithm);
        }

        return decryptor;
    }
}
