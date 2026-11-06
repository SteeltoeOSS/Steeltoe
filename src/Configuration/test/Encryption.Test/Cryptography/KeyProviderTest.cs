// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources.IO;
using Steeltoe.Configuration.Encryption.Cryptography;

namespace Steeltoe.Configuration.Encryption.Test.Cryptography;

public sealed class KeyProviderTest
{
    private const string KeyStorePath = "./Cryptography/server.jks";
    private const string KeyStorePassword = "letmein";

    [Fact]
    public void Closes_keystore_file_after_loading()
    {
        using var tempFile = new TempFile();
        File.Copy(KeyStorePath, tempFile.FullPath, true);

        _ = new KeyProvider(tempFile.FullPath, KeyStorePassword);

        AssertFileIsNotInUse(tempFile.FullPath);
    }

    [Fact]
    public void Closes_keystore_file_after_creating_decryptor_from_settings()
    {
        using var tempFile = new TempFile();
        File.Copy(KeyStorePath, tempFile.FullPath, true);

        var settings = new ConfigServerDecryptionSettings
        {
            Enabled = true,
            KeyStore =
            {
                Location = tempFile.FullPath,
                Password = KeyStorePassword,
                Alias = "mytestkey"
            }
        };

        TextDecryptorFactory.CreateDecryptor(settings).Should().BeOfType<RsaKeyStoreDecryptor>();

        AssertFileIsNotInUse(tempFile.FullPath);
    }

    private static void AssertFileIsNotInUse(string path)
    {
        // Fails with an IOException when another stream still has the file open.
        using var exclusiveStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        exclusiveStream.Length.Should().BeGreaterThan(0);
    }
}
