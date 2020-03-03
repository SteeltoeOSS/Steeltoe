// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            Thread.Sleep(2000);
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
            Thread.Sleep(2000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("key1", config["privateKey"]);
            Assert.True(changeCalled);

            token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");

            changeCalled = false;
            await File.WriteAllTextAsync(tempFile2, "barbar");
            Thread.Sleep(2000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("barbar", config["privateKey"]);
            Assert.True(changeCalled);
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
            Assert.Equal("instance.p12", config["certificate"]);
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
            Thread.Sleep(2000);

            // assert
            Assert.Equal("barfoo", await File.ReadAllTextAsync(config["certificate"]));
            Assert.True(changeCalled);

            // cleanup
            File.Delete(filename);
        }

        private async Task<string> CreateTempFileAsync(string contents)
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, contents);
            return tempFile;
        }
    }
}
