// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public class ConfigurationExtensionsTest
{
    [Fact]
    public void AddPemFiles_ThrowsOnNulls()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigurationExtensions.AddPemFiles(null, null, null));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationBuilder().AddPemFiles(null, null));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationBuilder().AddPemFiles("foobar", null));
    }

    [Fact]
    public void AddPemFiles_ReadsFiles()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles("instance.crt", "instance.key").Build();
        Assert.NotNull(configurationRoot["certificate"]);
        Assert.NotNull(configurationRoot["privateKey"]);
    }

    [Fact]
    public async Task AddPemFiles_ReloadsOnChange()
    {
        using var sandbox = new Sandbox();
        string tempFile1 = sandbox.CreateFile("cert", "cert");
        string tempFile2 = sandbox.CreateFile("key", "key");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(tempFile1, tempFile2).Build();

        Assert.Equal("cert", configurationRoot["certificate"]);
        Assert.Equal("key", configurationRoot["privateKey"]);

        await File.WriteAllTextAsync(tempFile1, "cert2");
        await Task.Delay(2000);

        if (configurationRoot["certificate"] == null)
        {
            // wait a little longer
            await Task.Delay(4000);
        }

        Assert.Equal("cert2", configurationRoot["certificate"]);
        Assert.Equal("key", configurationRoot["privateKey"]);
    }

    [Fact]
    public async Task AddPemFiles_NotifiesOnChange()
    {
        using var sandbox = new Sandbox();
        string tempFile1 = sandbox.CreateFile("cert", "cert1");
        string tempFile2 = sandbox.CreateFile("key", "key1");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(tempFile1, tempFile2).Build();

        bool changeCalled = false;
        IChangeToken token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");
        Assert.Equal("cert1", configurationRoot["certificate"]);
        Assert.Equal("key1", configurationRoot["privateKey"]);

        await File.WriteAllTextAsync(tempFile1, "barfoo");
        Thread.Sleep(4000);
        Assert.Equal("barfoo", configurationRoot["certificate"]);
        Assert.Equal("key1", configurationRoot["privateKey"]);
        Assert.True(changeCalled, "Change wasn't called for tempFile1");

        token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");

        changeCalled = false;
        await File.WriteAllTextAsync(tempFile2, "barbar");
        Thread.Sleep(4000);
        Assert.Equal("barfoo", configurationRoot["certificate"]);
        Assert.Equal("barbar", configurationRoot["privateKey"]);
        Assert.True(changeCalled, "Change wasn't called for tempFile2");
    }

    [Fact]
    public void AddCertificateFile_ThrowsOnNulls()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigurationExtensions.AddCertificateFile(null, null));
        Assert.Throws<ArgumentNullException>(() => new ConfigurationBuilder().AddCertificateFile(null));
    }

    [Fact]
    public void AddCertificateFile_HoldsPath()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificateFile("instance.p12").Build();
        Assert.Equal(Path.GetFullPath("instance.p12"), configurationRoot["certificate"]);
    }

    [Fact]
    public async Task AddCertificateFile_NotifiesOnChange()
    {
        // TODO: investigate why test fails when using Sandbox
        // see: https://github.com/SteeltoeOSS/Steeltoe/issues/736
        /*
        using var sandbox = new Sandbox();
        var filename = sandbox.CreateFile("fakeCertificate.p12", "cert1");
        */

        const string filename = "fakeCertificate.p12";
        await File.WriteAllTextAsync(filename, "cert1");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificateFile(filename).Build();

        bool changeCalled = false;
        IChangeToken token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");
        Assert.Equal("cert1", await File.ReadAllTextAsync(configurationRoot["certificate"]));

        await File.WriteAllTextAsync(filename, "barfoo");
        await Task.Delay(2000);

        Assert.Equal("barfoo", await File.ReadAllTextAsync(configurationRoot["certificate"]));
        Assert.True(changeCalled);

        // cleanup
        try
        {
            File.Delete(filename);
        }
        catch
        {
            // give it a second, try again
            await Task.Delay(1000);
            File.Delete(filename);
        }
    }
}
