// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Steeltoe.Configuration.Encryption.Decryption;

internal sealed class AesTextDecryptor : ITextDecryptor
{
    private const short KeySize = 256;
    private const short IvSize = 128;
    private readonly byte[] _key;
    private readonly IBufferedCipher _cipher;

    public AesTextDecryptor(string key)
        : this(key, RsaEncryptionSettings.DefaultSalt, false)
    {
    }

    public AesTextDecryptor(string key, string salt)
        : this(key, salt, false)
    {
    }

    public AesTextDecryptor(string key, string salt, bool strong)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(salt);

        _cipher = strong ? CipherUtilities.GetCipher("AES/GCM/NoPadding") : CipherUtilities.GetCipher("AES/CBC/PKCS5Padding");

        byte[] saltBytes = GetSaltBytes(salt);

        _key = KeyDerivation.Pbkdf2(key, saltBytes, KeyDerivationPrf.HMACSHA1, 1024, KeySize / 8);
    }

    private static byte[] GetSaltBytes(string salt)
    {
        try
        {
            return Convert.FromHexString(salt);
        }
        catch
        {
            return Encoding.Default.GetBytes(salt);
        }
    }

    public string Decrypt(string fullCipher)
    {
        byte[] bytes = Convert.FromHexString(fullCipher);
        return Decrypt(bytes);
    }

    public string Decrypt(string fullCipher, string alias)
    {
        throw new NotSupportedException("Key alias is not supported for symmetric encryption");
    }

    public string Decrypt(byte[] fullCipher)
    {
        ArgumentNullException.ThrowIfNull(fullCipher);

        byte[] iv = new byte[IvSize / 8];
        byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];

        using var ms = new MemoryStream(fullCipher);

        int readIv = ms.Read(iv);
        int readCipherBytes = ms.Read(cipherBytes);

        if (readIv != iv.Length || readCipherBytes != cipherBytes.Length)
        {
            throw new DecryptionException("Unexpected number of bytes read from cipher");
        }

        try
        {
            InitializeCipher(iv);

            byte[] clearTextBytes = _cipher.DoFinal(cipherBytes);
            return Encoding.Default.GetString(clearTextBytes);
        }
        catch (Exception exception)
        {
            throw new DecryptionException("Failed to decrypt", exception);
        }
    }

    private void InitializeCipher(byte[] iv)
    {
        var keyParam = new KeyParameter(_key);
        var keyParameters = new ParametersWithIV(keyParam, iv);
        _cipher.Init(false, keyParameters);
    }
}
