// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.ConfigServer;

public class Factory
{
    private ITextDecryptor CreateEncryptor(ConfigServerEncryptionSettings settings)
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
    //
    // public void te()
    // {
    //     if (convertedValue.StartsWith("{cipher}", StringComparison.InvariantCulture))
    //     {
    //         string cipher = convertedValue.Substring("{cipher}".Length);
    //         return _decryptor.Decrypt(cipher);
    //     }
    //
    //     return convertedValue;
    // }
}
