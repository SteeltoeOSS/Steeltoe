// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class ConfigurationExtensionsTest
    {
        [Fact]
        public void AddPemFiles_ThrowsOnNulls()
        {
            Assert.Throws<ArgumentNullException>(() => ConfigurationExtensions.AddPemFiles(null, null, null));
            Assert.Throws<ArgumentException>(() => ConfigurationExtensions.AddPemFiles(new ConfigurationBuilder(), null, null));
            Assert.Throws<ArgumentException>(() => ConfigurationExtensions.AddPemFiles(new ConfigurationBuilder(), "foobar", null));
        }

        [Fact]
        public void AddPemFiles_ReadsFiles()
        {
            var config = new ConfigurationBuilder()
                .AddPemFiles("instance.crt", "instance.key")
                .Build();
            Assert.NotNull(config["certificate"]);
            Assert.NotNull(config["privateKey"]);
        }

        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public async Task AddPemFiles_ReloadsOnChange()
        {
            var tempFile1 = await CreateTempFileAsync("cert");
            var tempFile2 = await CreateTempFileAsync("key");

            var config = new ConfigurationBuilder()
                .AddPemFiles(tempFile1, tempFile2)
                .Build();

            Assert.Equal("cert", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);

            await File.WriteAllTextAsync(tempFile1, "cert2");
            await Task.Delay(2000);
            if (config["certificate"] == null)
            {
                // wait a little longer
                await Task.Delay(4000);
            }

            Assert.Equal("cert2", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);
        }

        [Fact]
        public async Task AddPemFiles_NotifiesOnChange()
        {
            var tempFile1 = await CreateTempFileAsync("cert1");
            var tempFile2 = await CreateTempFileAsync("key1");

            var config = new ConfigurationBuilder()
                .AddPemFiles(tempFile1, tempFile2)
                .Build();

            var changeCalled = false;
            var token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");
            Assert.Equal("cert1", config["certificate"]);
            Assert.Equal("key1", config["privateKey"]);

            await File.WriteAllTextAsync(tempFile1, "barfoo");
            Thread.Sleep(4000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("key1", config["privateKey"]);
            Assert.True(changeCalled, "Change wasn't called for tempFile1");

            token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");

            changeCalled = false;
            await File.WriteAllTextAsync(tempFile2, "barbar");
            Thread.Sleep(4000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("barbar", config["privateKey"]);
            Assert.True(changeCalled, "Change wasn't called for tempFile2");
        }

        [Fact]
        public void AddCertificateFile_ThrowsOnNulls()
        {
            Assert.Throws<ArgumentNullException>(() => ConfigurationExtensions.AddCertificateFile(null, null));
            Assert.Throws<ArgumentException>(() => ConfigurationExtensions.AddCertificateFile(new ConfigurationBuilder(), null));
        }

        [Fact]
        public void AddCertificateFile_HoldsPath()
        {
            var config = new ConfigurationBuilder()
                .AddCertificateFile("instance.p12")
                .Build();
            Assert.Equal(Path.GetFullPath("instance.p12"), config["certificate"]);
        }

        [Fact]
        public async Task AddCertificateFile_NotifiesOnChange()
        {
            // arrange
            var filename = "fakeCertificate.p12";
            await File.WriteAllTextAsync(filename, "cert1");

            var config = new ConfigurationBuilder()
                .AddCertificateFile(filename)
                .Build();

            var changeCalled = false;
            var token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");
            Assert.Equal("cert1", await File.ReadAllTextAsync(config["certificate"]));

            // act
            await File.WriteAllTextAsync(filename, "barfoo");
            await Task.Delay(2000);

            // assert
            Assert.Equal("barfoo", await File.ReadAllTextAsync(config["certificate"]));
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

        private async Task<string> CreateTempFileAsync(string contents)
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, contents);
            return tempFile;
        }
    }
}
