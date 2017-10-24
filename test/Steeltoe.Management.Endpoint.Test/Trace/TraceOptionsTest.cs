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

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class TraceOptionsTest : BaseTest
    {
        [Fact]
        public void Constructor_InitializesWithDefaults()
        {
            var opts = new TraceOptions();
            Assert.True(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("trace", opts.Id);
            Assert.Equal(100, opts.Capacity);
            Assert.True(opts.AddTimeTaken);
            Assert.True(opts.AddRequestHeaders);
            Assert.True(opts.AddResponseHeaders);
            Assert.False(opts.AddPathInfo);
            Assert.False(opts.AddUserPrincipal);
            Assert.False(opts.AddParameters);
            Assert.False(opts.AddQueryString);
            Assert.False(opts.AddAuthType);
            Assert.False(opts.AddRemoteAddress);
            Assert.False(opts.AddSessionId);
        }

        [Fact]
        public void Contstructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;
            Assert.Throws<ArgumentNullException>(() => new TraceOptions(config));
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
            'trace' : {
                'enabled': true,
                'sensitive': true,
                'capacity': 1000,
                'addTimeTaken' : false,
                'addRequestHeaders': false,
                'addResponseHeaders': false,
                'addPathInfo': true,
                'addUserPrincipal': true,
                'addParameters': true,
                'addQueryString': true,
                'addAuthType': true,
                'addRemoteAddress': true,
                'addSessionId': true
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

            var opts = new TraceOptions(config);
            CloudFoundryOptions cloudOpts = new CloudFoundryOptions(config);

            Assert.True(cloudOpts.Enabled);
            Assert.False(cloudOpts.Sensitive);
            Assert.Equal(string.Empty, cloudOpts.Id);
            Assert.Equal("/cloudfoundryapplication", cloudOpts.Path);
            Assert.True(cloudOpts.ValidateCertificates);

            Assert.True(opts.Enabled);
            Assert.True(opts.Sensitive);
            Assert.Equal("trace", opts.Id);
            Assert.Equal("/cloudfoundryapplication/trace", opts.Path);
            Assert.Equal(1000, opts.Capacity);
            Assert.False(opts.AddTimeTaken);
            Assert.False(opts.AddRequestHeaders);
            Assert.False(opts.AddResponseHeaders);
            Assert.True(opts.AddPathInfo);
            Assert.True(opts.AddUserPrincipal);
            Assert.True(opts.AddParameters);
            Assert.True(opts.AddQueryString);
            Assert.True(opts.AddAuthType);
            Assert.True(opts.AddRemoteAddress);
            Assert.True(opts.AddSessionId);
        }
    }
}
