// Licensed to the .NET Foundation under one or more agreements.
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
    private static short _keySize = 256;
    private static short _ivSize = 128;
    private readonly byte[] _key;
    private readonly IBufferedCipher _cipher;

    public AesTextDecryptor(string key, string salt = "deadbeef", bool strong = false)
    {
        _cipher = strong ? CipherUtilities.GetCipher("AES/GCM/NoPadding") : CipherUtilities.GetCipher("AES/CBC/PKCS5Padding");

        byte[] saltBytes = GetSaltBytes(salt);

        _key = KeyDerivation.Pbkdf2(key, saltBytes, KeyDerivationPrf.HMACSHA1, 1024, _keySize / 8);
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

    public string Decrypt(byte[] fullCipher)
    {
        byte[] iv = new byte[_ivSize / 8];
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
