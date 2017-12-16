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

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class JsonStreamConfigurationSourceTest
    {
        [Fact]
        public void Constructor_Throws_StreamNull()
        {
            // Arrange
            MemoryStream stream = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new JsonStreamConfigurationSource(stream));
            Assert.Contains(nameof(stream), ex.Message);
        }

        [Fact]
        public void Build_WithStreamSource_ReturnsExpected()
        {
            // Arrange
            var environment = @"
{
 
  'application_id': 'fa05c1a9-0fc1-4fbd-bae1-139850dec7a3',
  'application_name': 'my-app',
  'application_uris': [
    'my-app.10.244.0.34.xip.io'
  ],
  'application_version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca',
  'limits': {
    'disk': 1024,
    'fds': 16384,
    'mem': 256
  },
  'name': 'my-app',
  'space_id': '06450c72-4669-4dc6-8096-45f9777db68a',
  'space_name': 'my-space',
  'uris': [
    'my-app.10.244.0.34.xip.io',
    'my-app2.10.244.0.34.xip.io'
  ],
  'users': null,
  'version': 'fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca'
  }";
            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(environment);
            var source = new JsonStreamConfigurationSource(memStream);
            var provider = new JsonStreamConfigurationProvider(source);
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.Add(source);
            var root = builder.Build();

            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", root["application_id"]);
            Assert.Equal("1024", root["limits:disk"]);
            Assert.Equal("my-app.10.244.0.34.xip.io", root["uris:0"]);
            Assert.Equal("my-app2.10.244.0.34.xip.io", root["uris:1"]);
        }
    }
}
