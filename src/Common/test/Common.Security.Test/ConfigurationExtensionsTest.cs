// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Options;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Common.Security.Test;

public sealed class ConfigurationExtensionsTest
{
    private const string CertificateName = "test";

    [Fact]
    public void AddPemFiles_ReadsFiles()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(CertificateName, "instance.crt", "instance.key").Build();
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().NotBeNullOrEmpty();
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddPemFiles_ReloadsOnChange()
    {
        using var sandbox = new Sandbox();
        string tempFile1 = sandbox.CreateFile("cert", "cert");
        string tempFile2 = sandbox.CreateFile("key", "key");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(CertificateName, tempFile1, tempFile2).Build();

        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo("cert");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().BeEquivalentTo("key");

        await File.WriteAllTextAsync(tempFile1, "cert2");
        await Task.Delay(2000);

        if (configurationRoot["certificate"] == null)
        {
            // wait a little longer
            await Task.Delay(4000);
        }

        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo("cert2");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().BeEquivalentTo("key");
    }

    [Fact]
    public async Task AddPemFiles_NotifiesOnChange()
    {
        using var sandbox = new Sandbox();
        string tempFile1 = sandbox.CreateFile("cert", "cert1");
        string tempFile2 = sandbox.CreateFile("key", "key1");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddPemFiles(CertificateName, tempFile1, tempFile2).Build();

        bool changeCalled = false;
        IChangeToken token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo("cert1");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().BeEquivalentTo("key1");

        await File.WriteAllTextAsync(tempFile1, "barfoo");
        await Task.Delay(4000);
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo("barfoo");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().BeEquivalentTo("key1");
        changeCalled.Should().BeTrue("Change wasn't called for tempFile1");

        token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");

        changeCalled = false;
        await File.WriteAllTextAsync(tempFile2, "barbar");
        await Task.Delay(4000);
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo("barfoo");
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:privateKey"].Should().BeEquivalentTo("barbar");
        changeCalled.Should().BeTrue("Change wasn't called for tempFile2");
    }

    [Fact]
    public void AddCertificateFile_HoldsPath()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificateFile(CertificateName, "instance.p12").Build();
        configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"].Should().BeEquivalentTo(Path.GetFullPath("instance.p12"));
    }

    [Fact]
    public async Task AddCertificateFile_NotifiesOnChange()
    {
        const string filename = "fakeCertificate.p12";
        await File.WriteAllTextAsync(filename, "cert1");

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCertificateFile(CertificateName, filename).Build();

        bool changeCalled = false;
        IChangeToken token = configurationRoot.GetReloadToken();
        token.RegisterChangeCallback(_ => changeCalled = true, "state");

        (await File.ReadAllTextAsync(configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"]!)).Should()
            .BeEquivalentTo("cert1");

        await File.WriteAllTextAsync(filename, "barfoo");
        await Task.Delay(2000);

        (await File.ReadAllTextAsync(configurationRoot[$"{CertificateOptions.ConfigurationPrefix}:{CertificateName}:certificate"]!)).Should()
            .BeEquivalentTo("barfoo");

        changeCalled.Should().BeTrue();

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
