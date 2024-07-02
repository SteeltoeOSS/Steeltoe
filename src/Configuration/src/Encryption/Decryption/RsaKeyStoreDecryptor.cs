// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption.Decryption;

internal sealed class RsaKeyStoreDecryptor : ITextDecryptor
{
    private readonly string _salt;
    private readonly bool _strong;
    private readonly IKeyProvider _keyProvider;
    private readonly IBufferedCipher _cipher;
    private readonly string _defaultKeyAlias;

    public RsaKeyStoreDecryptor(IKeyProvider keyProvider, string alias)
        : this(keyProvider, alias, RsaEncryptionSettings.DefaultSalt, false, RsaEncryptionSettings.DefaultAlgorithm)
    {
    }

    public RsaKeyStoreDecryptor(IKeyProvider keyProvider, string alias, string salt, bool strong, string algorithm)
    {
        ArgumentGuard.NotNull(keyProvider);
        ArgumentGuard.NotNull(alias);
        ArgumentGuard.NotNull(salt);
        ArgumentGuard.NotNull(strong);
        ArgumentGuard.NotNull(algorithm);

        _salt = salt;
        _defaultKeyAlias = alias;
        _strong = strong;
        _keyProvider = keyProvider;
        _cipher = CreateCipher(algorithm);
    }

    private IBufferedCipher CreateCipher(string algorithm)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "DEFAULT" => CipherUtilities.GetCipher("RSA/NONE/PKCS1Padding"),
            "OAEP" => CipherUtilities.GetCipher("RSA/ECB/PKCS1"),
            _ => throw new ArgumentException("algorithm should be one of DEFAULT or OAEP")
        };
    }

    public string Decrypt(string fullCipher)
    {
        byte[] bytes = Convert.FromBase64String(fullCipher);
        return Decrypt(bytes, _defaultKeyAlias);
    }

    public string Decrypt(string fullCipher, string alias)
    {
        byte[] bytes = Convert.FromBase64String(fullCipher);
        return Decrypt(bytes, alias);
    }

    private string Decrypt(byte[] fullCipher, string alias)
    {
        ArgumentGuard.NotNull(fullCipher);

        ICipherParameters? key = _keyProvider.GetKey(alias);

        if (key == null)
        {
            throw new DecryptionException($"Key {alias} does not exist in keystore.");
        }

        _cipher.Init(false, key);
        using var ms = new MemoryStream(fullCipher);

        int secretLength = ReadSecretLength(ms);
        byte[] secretBytes = new byte[secretLength];
        byte[] cipherTextBytes = new byte[fullCipher.Length - secretBytes.Length - 2];

        int secretBytesRead = ms.Read(secretBytes);
        int cipherBytesRead = ms.Read(cipherTextBytes);

        if (secretBytesRead != secretBytes.Length || cipherBytesRead != cipherTextBytes.Length)
        {
            throw new DecryptionException("Unexpected number of bytes read from cipher");
        }

        try
        {
            // Spring Cloud Config hex string is lower case
#pragma warning disable S4040 // Strings should be normalized to uppercase
            string secret = Convert.ToHexString(_cipher.DoFinal(secretBytes)).ToLowerInvariant();
#pragma warning restore S4040 // Strings should be normalized to uppercase
            var decryptor = new AesTextDecryptor(secret, _salt, _strong);
            return decryptor.Decrypt(cipherTextBytes);
        }
        catch (Exception exception)
        {
            throw new DecryptionException("Failed to decrypt cipher", exception);
        }
    }

    private int ReadSecretLength(Stream ms)
    {
        byte[] lengthBytes = new byte[2];
        int read = ms.Read(lengthBytes);

        if (read != lengthBytes.Length)
        {
            throw new DecryptionException("Unexpected number of bytes read from cipher");
        }

        return BinaryPrimitives.ReadInt16BigEndian(lengthBytes);
    }
}
