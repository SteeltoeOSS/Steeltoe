// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Steeltoe.Configuration.Encryption.ConfigServer;

public sealed class RsaKeyStoreDecryptor : ITextDecryptor
{
    private readonly string _salt;
    private readonly bool _strong;
    private readonly IKeyProvider _keyProvider;
    private readonly IBufferedCipher _cipher;
    private readonly string _defaultKeyAlias;

    public RsaKeyStoreDecryptor(IKeyProvider keyProvider, string alias, string salt = "deadbeaf", bool strong = false, string algorithm = "DEFAULT")
    {
        _salt = salt;
        _defaultKeyAlias = alias;
        _strong = strong;
        _keyProvider = keyProvider;
        _cipher = GetCipher(algorithm);
    }

    private IBufferedCipher GetCipher(string algorithm)
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
        ICipherParameters key = _keyProvider.GetKey(alias);

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
#pragma warning disable S4040
            // Spring Cloud Config hex string is lower case
            string secret = Convert.ToHexString(_cipher.DoFinal(secretBytes)).ToLowerInvariant();
#pragma warning restore S4040
            var decryptor = new AesTextDecryptor(secret, _salt, _strong);
            return decryptor.Decrypt(cipherTextBytes);
        }
        catch (Exception ex)
        {
            throw new DecryptionException("Failed to decrypt cipher", ex);
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
