// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration.Encryption.Decryption;
using Xunit;

namespace Steeltoe.Configuration.Encryption.Test.Decryption;

public sealed class AesTextDecryptorTests
{
    [Theory]
    [MemberData(nameof(GetTestVector), 4)]
    public void DecodeTestForSpringConfigCipher(string salt, string key, string cipher, string plainText)
    {
        var textDecryptor = new AesTextDecryptor(key, salt);
        string decrypted = textDecryptor.Decrypt(cipher);
        Assert.Equal(plainText, decrypted);
    }

    public static IEnumerable<object[]> GetTestVector(int numTests)
    {
        var allData = new List<object[]>
        {
            new[]
            {
                "deadbeef",
                "12345678901234567890",
                "23f97efeed4ab62294432e8ef6b2905e336c245ecb1d5122b2c288c4deeae1b737952312e97e2cf013dd31a28fc60704",
                "encrypt the world"
            },
            new[]
            {
                // from Spring Cloud Config documentation
                "deadbeef",
                "foo",
                "682bc583f4641835fa2db009355293665d2647dade3375c0ee201de2a49f7bda",
                "mysecret"
            },
            new[]
            {
                "deadbeef",
                "12345678901234567890",
                "e31b13ab248f96f3cc22be5942d9ebec19a6b50318b2f5d30ea515064971bdebff6974890197626f0dcd5b648950e96f",
                "encrypt the world"
            },
            new[]
            {
                "deadbeef",
                "12345678901234567890",
                "e401ca0578839c9e5207f52d0ae4dc836f8c6530cdc90f14b544180f6fdb9265b80d6ace9fbbab700c7af32141171358",
                "encrypt the world"
            }
        };

        return allData.Take(numTests);
    }
}
