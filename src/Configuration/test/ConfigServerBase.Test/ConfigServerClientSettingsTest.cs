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

using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerClientSettingsTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            // Arrange
            var settings = new ConfigServerClientSettings();

            // Act and Assert
            TestHelper.VerifyDefaults(settings);
        }

        [Fact]
        public void GetRawUris_GoodWithUserPass()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("https://localhost:8888/", settings.RawUris[0]);
        }

        [Fact]
        public void GetRawUris_MultipleUris_GoodWithUserPass()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/, https://user:pass@localhost:9999/" };

            // Act and Assert
            Assert.Equal("https://localhost:8888/", settings.RawUris[0]);
            Assert.Equal("https://localhost:9999/", settings.RawUris[1]);
        }

        [Fact]
        public void GetRawUris_Bad()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "blahblah" };

            // Act and Assert
            Assert.Empty(settings.RawUris);
        }

        [Fact]
        public void GetUserName_GoodWithUserPassOnUri()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("user", settings.Username);
        }

        [Fact]
        public void GetUserName_MultipleUrisWithUserPass_ReturnsNull()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/" };

            // Act and Assert
            Assert.Null(settings.Username);
        }

        [Fact]
        public void GetUserName_MultipleUrisWithUserPass_ReturnsUserNameSetting()
        {
            // Arrange
            var settings = new ConfigServerClientSettings()
            {
                Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
                Username = "user"
            };

            // Act and Assert
            Assert.Equal("user", settings.Username);
        }

        [Fact]
        public void GetPassword_GoodWithUserPassOnUri()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("pass", settings.Password);
        }

        [Fact]
        public void GetPassword_MultipleUrisWithUserPass_ReturnsNull()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/" };

            // Act and Assert
            Assert.Null(settings.Password);
        }

        [Fact]
        public void GetPassword_MultipleUrisWithUserPass_ReturnsPasswordSetting()
        {
            // Arrange
            var settings = new ConfigServerClientSettings()
            {
                Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
                Password = "password"
            };

            // Act and Assert
            Assert.Equal("password", settings.Password);
        }

        [Fact]
        public void GetUserName_GoodWithUserPassOnUri_SettingsOverrides()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/", Username = "explicitOverrides" };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Username);
            Assert.Equal("pass", settings.Password);
        }

        [Fact]
        public void GetPassword_GoodWithUserPassOnUri_SettingsOverrides()
        {
            // Arrange
            var settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/", Password = "explicitOverrides" };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Password);
            Assert.Equal("user", settings.Username);
        }

        [Fact]
        public void GetUserName_MultipleUrisWithUserPass_SettingsUsed()
        {
            // Arrange
            var settings = new ConfigServerClientSettings()
            {
                Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
                Username = "explicitOverrides"
            };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Username);
            Assert.Null(settings.Password);
        }

        [Fact]
        public void GetPassword_MultipleUrisWithUserPass_SettingsOverrides()
        {
            // Arrange
            var settings = new ConfigServerClientSettings()
            {
                Uri = "https://user:pass@localhost:8888/, https://user1:pass1@localhost:9999/",
                Password = "explicitOverrides"
            };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Password);
            Assert.Null(settings.Username);
        }
    }
}
