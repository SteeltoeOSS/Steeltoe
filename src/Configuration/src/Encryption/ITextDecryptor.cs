// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.Encryption;

public interface ITextDecryptor
{
    string Decrypt(string fullCipher);

#pragma warning disable CA1716 // Identifiers should not match keywords
    string Decrypt(string fullCipher, string alias);
#pragma warning restore CA1716 // Identifiers should not match keywords
}
