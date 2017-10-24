// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test
{
    public class ManagementOptionsTest : BaseTest
    {
        [Fact]
        public void InitializedWithDefaults()
        {
            ManagementOptions opts = ManagementOptions.GetInstance();
            Assert.False(opts.Enabled.HasValue);
            Assert.False(opts.Sensitive.HasValue);
            Assert.Equal("/", opts.Path);
        }

        [Fact]
        public void ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => ManagementOptions.GetInstance(config));
        }

        [Fact]
        public void BindsConfigurationCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/management',
            'info' : {
                'enabled': true,
                'sensitive': true,
                'id': '/infomanagement'
            }
        }
    }
}";
            var path = TestHelpers.CreateTempFile(appsettings);
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            ManagementOptions opts = ManagementOptions.GetInstance(config);
            Assert.False(opts.Enabled);
            Assert.False(opts.Sensitive);
            Assert.Equal("/management", opts.Path);
        }
    }
}
