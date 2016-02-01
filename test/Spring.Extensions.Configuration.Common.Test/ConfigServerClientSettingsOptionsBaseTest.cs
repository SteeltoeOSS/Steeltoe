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

using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Xunit;

namespace Spring.Extensions.Configuration.Common.Test
{
    public class ConfigServerClientSettingsOptionsTest
    {
        [Fact]
        public void ConfigureConfigServerClientSettingsOptions_WithDefaults()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();
            var settings = new ConfigServerClientSettingsBase();


            // Act and Assert
            var builder = new ConfigurationBuilder();
            builder.Add(new ConfigServerConfigurationProviderBase(settings));
            var config = builder.Build();


            services.Configure<ConfigServerClientSettingsOptionsBase>(config);
            var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptionsBase>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            ConfigServerTestHelpers.VerifyDefaults(options.Settings);

            Assert.False(options.Enabled);
            Assert.False(options.FailFast);
            Assert.Null(options.Uri);
            Assert.Null(options.Environment);
            Assert.False(options.ValidateCertificates);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);

        }
    }
}
