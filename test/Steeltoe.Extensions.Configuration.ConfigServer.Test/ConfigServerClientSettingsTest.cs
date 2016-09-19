//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class ConfigServerClientSettingsTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            // Act and Assert
            TestHelpers.VerifyDefaults(settings);

        }

        [Fact]
        public void GetRawUri_GoodWithUserPass()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("https://localhost:8888/", settings.RawUri);

        }
        [Fact]
        public void GetRawUri_Bad()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "blahblah" };

            // Act and Assert
            Assert.Equal("blahblah", settings.RawUri);

        }
        [Fact]
        public void GetUserName_GoodWithUserPassOnUri()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("user", settings.Username);

        }
        [Fact]
        public void GetPassword_GoodWithUserPassOnUri()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/" };

            // Act and Assert
            Assert.Equal("pass", settings.Password);

        }
        [Fact]
        public void GetUserName_GoodWithUserPassOnUri_SettingsOverrides()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/", Username = "explicitOverrides" };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Username);
            Assert.Equal("pass", settings.Password);

        }
        [Fact]
        public void GetPassword_GoodWithUserPassOnUri_SettingsOverrides()
        {
            // Arrange
            ConfigServerClientSettings settings = new ConfigServerClientSettings() { Uri = "https://user:pass@localhost:8888/", Password = "explicitOverrides" };

            // Act and Assert
            Assert.Equal("explicitOverrides", settings.Password);
            Assert.Equal("user", settings.Username);

        }


    }

}
