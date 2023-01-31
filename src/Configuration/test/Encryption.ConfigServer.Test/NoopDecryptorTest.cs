// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Configuration.Encryption.ConfigServer.Test;

public class NoopDecryptorTest
{
    [Fact]
    public void TestNoopEncryptor_ReturnsInput()
    {
        var noopDecryptor = new NoopDecryptor();
        Assert.Equal("something", noopDecryptor.Decrypt("something"));
        Assert.Equal("something", noopDecryptor.Decrypt("something", "anything"));
    }
}
