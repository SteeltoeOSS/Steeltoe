// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Decryption;

internal static class EncryptionFactory
{
    public static ITextDecryptor CreateEncryptor(ConfigServerEncryptionSettings settings)
    {
        ITextDecryptor decryptor = new NoopDecryptor();

        if (settings.Enabled)
        {
            EnsureValidEncryptionSettings(settings);

            if (!string.IsNullOrEmpty(settings.Key))
            {
                return new AesTextDecryptor(settings.Key);
            }

            if (settings is { KeyStore: { Location: not null, Password: not null, Alias: not null }, Rsa: { Salt: not null, Algorithm: not null } })
            {
                var keystoreStream = new FileStream(settings.KeyStore.Location, FileMode.Open, FileAccess.Read);
                var keyProvider = new KeyProvider(keystoreStream, settings.KeyStore.Password);

                return new RsaKeyStoreDecryptor(keyProvider, settings.KeyStore.Alias, settings.Rsa.Salt, settings.Rsa.Strong, settings.Rsa.Algorithm);
            }
        }

        return decryptor;
    }

    private static void EnsureValidEncryptionSettings(ConfigServerEncryptionSettings settings)
    {
        bool hasKeySettings = !string.IsNullOrEmpty(settings.Key);

        bool hasKeyStoreSettings = !string.IsNullOrEmpty(settings.KeyStore.Location) && !string.IsNullOrEmpty(settings.KeyStore.Password) &&
            !string.IsNullOrEmpty(settings.KeyStore.Alias);

        if ((hasKeySettings && hasKeyStoreSettings) || (!hasKeySettings && !hasKeyStoreSettings))
        {
            throw new DecryptionException(
                "No valid configuration for encryption key or key store. Either 'encrypt.key' or the 'encrypt.keyStore' properties (location, password, alias) must be set. Not both.");
        }
    }
}
