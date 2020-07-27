// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Steeltoe.Common.Security.Test
{
    public class PemConfigurationExtensionsTest
    {
        [Fact]
        public void AddPemFiles_ThrowsOnNulls()
        {
            Assert.Throws<ArgumentNullException>(() => PemConfigurationExtensions.AddPemFiles(null, null, null));
            Assert.Throws<ArgumentException>(() => PemConfigurationExtensions.AddPemFiles(new ConfigurationBuilder(), null, null));
            Assert.Throws<ArgumentException>(() => PemConfigurationExtensions.AddPemFiles(new ConfigurationBuilder(), "foobar", null));
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
        public void AddPemFiles_ReloadsOnChange()
        {
            var tempFile1 = CreateTempFile("cert");
            var tempFile2 = CreateTempFile("key");

            var config = new ConfigurationBuilder()
                .AddPemFiles(tempFile1, tempFile2)
                .Build();

            Assert.Equal("cert", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);

            File.WriteAllText(tempFile1, "cert2");
            Thread.Sleep(2000);
            Assert.Equal("cert2", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);
        }

        [Fact]
        public void AddPemFiles_NotifiesOnChange()
        {
            var tempFile1 = CreateTempFile("cert1");
            var tempFile2 = CreateTempFile("key1");

            var config = new ConfigurationBuilder()
                .AddPemFiles(tempFile1, tempFile2)
                .Build();

            var changeCalled = false;
            var token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");
            Assert.Equal("cert1", config["certificate"]);
            Assert.Equal("key1", config["privateKey"]);

            File.WriteAllText(tempFile1, "barfoo");
            Thread.Sleep(2000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("key1", config["privateKey"]);
            Assert.True(changeCalled);

            token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");

            changeCalled = false;
            File.WriteAllText(tempFile2, "barbar");
            Thread.Sleep(2000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("barbar", config["privateKey"]);
            Assert.True(changeCalled);
        }

        private string CreateTempFile(string contents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
    }
}
