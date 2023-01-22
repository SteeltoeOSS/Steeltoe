// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Data;
using System.Globalization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Steeltoe.Configuration.ConfigServer.Encryption;

internal sealed class RsaKeyStoreDecryptor : ITextDecryptor
{
    private readonly string _salt;
    private readonly bool _strong;
    private readonly IKeyProvider _keyProvider;
    private readonly IBufferedCipher _cipher;
    private readonly string _defaultKeyAlias;

    public RsaKeyStoreDecryptor(IKeyProvider keyProvider, string alias, string salt = "deadbeaf",
        bool strong = false, string algorithm = "DEFAULT")
    {
        _salt = salt;
        _defaultKeyAlias = alias;
        _strong = strong;
        _keyProvider = keyProvider;
        _cipher = GetCyper(algorithm);
    }

    public IBufferedCipher GetCyper(string algorithm)
    {
        switch (algorithm.ToUpperInvariant())
        {
            case "DEFAULT":
                return CipherUtilities.GetCipher("RSA/NONE/PKCS1Padding");
            case "OAEP":
                return CipherUtilities.GetCipher("RSA/ECB/PKCS1");
            default:
                throw new ArgumentException("algorithm should be one of DEFAULT or OAEP");
        }
    }

    public string Decrypt(string cipher)
    {
        var fullCipher = Convert.FromBase64String(cipher);
        return Decrypt(fullCipher);
    }

    public string Decrypt(byte[] fullCipher)
    {
        return Decrypt(fullCipher, _defaultKeyAlias);
    }

    public string Decrypt(byte[] fullCipher, string alias)
    {
        _cipher.Init(false, _keyProvider.GetKey(alias));
        using var ms = new MemoryStream(fullCipher);

        int secretLength = ReadSecretLenght(ms);
        byte[] secretBytes = new byte[secretLength];
        byte[] cipherTextBytes = new byte[fullCipher.Length - secretBytes.Length - 2];

        if (ms.Read(secretBytes) != secretBytes.Length)
        {
            throw new DecryptionException($"Failed to read {nameof(secretBytes)} from encrypted text");
        }

        if (ms.Read(cipherTextBytes) != cipherTextBytes.Length)
        {
            throw new DecryptionException($"Failed to read {nameof(cipherTextBytes)} from encrypted text");
        }

        try
        {
            string secret = Convert.ToHexString(_cipher.DoFinal(secretBytes)).ToLowerInvariant();
            AesTextDecryptor decryptor = new AesTextDecryptor(secret, salt: _salt, strong: _strong);
            return decryptor.Decrypt(cipherTextBytes);
        }
        catch (Exception ex)
        {
            throw new DecryptionException("Failed to decrypt cipher", ex);
        }
    }

    private int ReadSecretLenght(MemoryStream ms)
    {
        byte[] length = new byte[2];
        if (ms.Read(length) != length.Length)
        {
            throw new DecryptionException($"Failed to read {nameof(length)} from encrypted text");
        }
        return BinaryPrimitives.ReadInt16BigEndian(length);
    }
}
