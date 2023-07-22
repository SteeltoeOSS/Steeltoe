// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption.Decryption;

internal static class EncryptionFactory
{
    public static ITextDecryptor CreateEncryptor(ConfigServerEncryptionSettings settings)
    {
        ITextDecryptor decryptor = new NoopDecryptor();

        if (settings.EncryptionEnabled)
        {
            EnsureValidEncryptionSettings(settings);
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

    private static void EnsureValidEncryptionSettings(ConfigServerEncryptionSettings settings)
    {
        bool validEncryptionKeySettings = !string.IsNullOrEmpty(settings.EncryptionKey);
        bool validKeystoreSettings =
            !string.IsNullOrEmpty(settings.EncryptionKeyStoreLocation) &&
            !string.IsNullOrEmpty(settings.EncryptionKeyStorePassword) &&
            !string.IsNullOrEmpty(settings.EncryptionKeyStoreAlias);

        if (!(validEncryptionKeySettings ^ validKeystoreSettings))
        {
            throw new DecryptionException("No valid configuration for encryption key or key store. Either 'encrypt.key' or the 'encrypt.keyStore' properties (location, password, alias) should be set. Not both.");
        }
    }
}
