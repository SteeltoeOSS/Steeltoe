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
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Loggers.Test
{
    public class LoggersOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new LoggersOptions();
            Assert.True(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("loggers", opts.Id);
        }

        [Fact]
        public void Contstructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new LoggersOptions(config));
        }

        [Fact]
        public void Contstructor_BindsConfigurationCorrectly()
        {
            var appsettings = @"
{
    'management': {
        'endpoints': {
            'enabled': false,
            'sensitive': false,
            'path': '/cloudfoundryapplication',
            'loggers' : {
                'enabled': false,
                'sensitive' : true
            },
            'cloudfoundry': {
                'validatecertificates' : true,
                'enabled': true
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

            var opts = new LoggersOptions(config);
            CloudFoundryOptions cloudOpts = new CloudFoundryOptions(config);

            Assert.True(cloudOpts.Enabled);
            Assert.False(cloudOpts.Sensitive);
            Assert.Equal(string.Empty, cloudOpts.Id);
            Assert.Equal("/cloudfoundryapplication", cloudOpts.Path);
            Assert.True(cloudOpts.ValidateCertificates);

            Assert.False(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("loggers", opts.Id);
            Assert.Equal("/cloudfoundryapplication/loggers", opts.Path);
        }
    }
}
