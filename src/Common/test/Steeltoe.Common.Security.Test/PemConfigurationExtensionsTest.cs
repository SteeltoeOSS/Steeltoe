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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            var tempFile1 = CreateTempFile("cert");
            var tempFile2 = CreateTempFile("key");

            var config = new ConfigurationBuilder()
                .AddPemFiles(tempFile1, tempFile2)
                .Build();

            bool changeCalled = false;
            var token = config.GetReloadToken();
            token.RegisterChangeCallback((o) => changeCalled = true, "state");
            Assert.Equal("cert", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);

            File.WriteAllText(tempFile1, "barfoo");
            Thread.Sleep(2000);
            Assert.Equal("barfoo", config["certificate"]);
            Assert.Equal("key", config["privateKey"]);
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

        private static string CreateTempFile(string contents)
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, contents);
            return tempFile;
        }
    }
}
