﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Steeltoe.Configuration.ConfigServer.Encryption;

internal sealed class AesTextDecryptor : ITextDecryptor
{
    private readonly short KEYSIZE = 256;
    private readonly byte[] _key;
    private readonly IBufferedCipher _cipher;

    public AesTextDecryptor(string key, string salt = "deadbeef", bool strong = false)
    {
        _cipher = strong
            ? CipherUtilities.GetCipher("AES/GCM/NoPadding")
            : CipherUtilities.GetCipher("AES/CBC/PKCS5Padding");

        byte[] saltBytes = GetSaltBytes(salt);

        _key =  KeyDerivation.Pbkdf2(key, saltBytes, KeyDerivationPrf.HMACSHA1, 1024, KEYSIZE / 8);
    }

    private static byte[] GetSaltBytes(string salt)
    {
        try
        {
            return Convert.FromHexString(salt);
        }
        catch
        {
            return UTF8Encoding.Default.GetBytes(salt);
        }
    }

    public string Decrypt(string cipher)
    {
        var fullCipher = Convert.FromHexString(cipher);

        return Decrypt(fullCipher);
    }

    public string Decrypt(byte[] fullCipher)
    {
        byte[] iv = new byte[16];
        byte[] cipherBytes = new byte[fullCipher.Length - 16];

        using var ms = new MemoryStream(fullCipher);

        if (ms.Read(iv) != iv.Length)
        {
            throw new DecryptionException($"Failed to read {nameof(iv)} from encrypted text");
        }

        if (ms.Read(cipherBytes) != cipherBytes.Length)
        {
            throw new DecryptionException($"Failed to read {nameof(cipherBytes)} from encrypted text");
        }

        try
        {
            InitializeCipher(iv);

            byte[] clearTextBytes = _cipher.DoFinal(cipherBytes);
            return UTF8Encoding.Default.GetString(clearTextBytes);
        }
        catch (Exception ex)
        {
            throw new DecryptionException("Failed to decrypt", ex);
        }
    }

    public string Decrypt(byte[] fullCipher, string alias)
    {
        throw new NotSupportedException();
    }

    private void InitializeCipher(byte[] iv)
    {
        var keyParam = new KeyParameter(_key);
        var keyParameters = new ParametersWithIV(keyParam, iv);
        _cipher.Init(false, keyParameters);
    }
}
