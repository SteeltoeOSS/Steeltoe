// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption.Test.Cryptography;

public sealed class NoneDecryptorTest
{
    [Fact]
    public void TestNoneEncryptor_ReturnsInput()
    {
        var noneDecryptor = new NoneDecryptor();

        noneDecryptor.Decrypt("something").Should().Be("something");
        noneDecryptor.Decrypt("something", "anything").Should().Be("something");
    }
}
