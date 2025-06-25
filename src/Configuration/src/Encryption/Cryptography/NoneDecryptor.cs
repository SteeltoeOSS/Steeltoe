// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption.Cryptography;

internal sealed class NoneDecryptor : ITextDecryptor
{
    public string Decrypt(string fullCipher)
    {
        return fullCipher;
    }

    public string Decrypt(string fullCipher, string alias)
    {
        return fullCipher;
    }
}
