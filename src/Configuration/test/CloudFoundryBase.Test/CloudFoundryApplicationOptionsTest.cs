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
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test
{
    public class CloudFoundryApplicationOptionsTest
    {
        [Fact]
        public void Constructor_WithNoVcapApplicationConfiguration()
        {
            var builder = new ConfigurationBuilder();
            var config = builder.Build();

            var options = new CloudFoundryApplicationOptions(config);

            Assert.Null(options.CF_Api);
            Assert.Null(options.ApplicationId);
            Assert.Null(options.Application_Id);
            Assert.Null(options.ApplicationName);
            Assert.Null(options.Application_Name);
            Assert.Null(options.ApplicationUris);
            Assert.Null(options.Application_Uris);
            Assert.Null(options.ApplicationVersion);
            Assert.Null(options.Application_Version);
            Assert.Null(options.InstanceId);
            Assert.Null(options.Instance_Id);
            Assert.Equal(-1, options.InstanceIndex);
            Assert.Equal(-1, options.Instance_Index);
            Assert.Null(options.Limits);
            Assert.Null(options.Name);
            Assert.Null(options.SpaceId);
            Assert.Null(options.Space_Id);
            Assert.Null(options.SpaceName);
            Assert.Null(options.Space_Name);
            Assert.Null(options.Start);
            Assert.Null(options.Uris);
            Assert.Null(options.Version);
            Assert.Null(options.Instance_IP);
            Assert.Null(options.InstanceIP);
            Assert.Null(options.Internal_IP);
            Assert.Null(options.InternalIP);
            Assert.Equal(-1, options.DiskLimit);
            Assert.Equal(-1, options.FileDescriptorLimit);
            Assert.Equal(-1, options.InstanceIndex);
            Assert.Equal(-1, options.MemoryLimit);
            Assert.Equal(-1, options.Port);
        }

        [Fact]
        public void Constructor_WithVcapApplicationConfiguration()
        {
            // Arrange
            var configJson = @"
            {
                ""vcap"": {
                    ""application"" : {
                        ""cf_api"": ""https://api.system.testcloud.com"",
                        ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                        ""application_name"": ""my-app"",
                        ""application_uris"": [
                            ""my-app.10.244.0.34.xip.io""
                        ],
                        ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                        ""limits"": {
                            ""disk"": 1024,
                            ""fds"": 16384,
                            ""mem"": 256
                        },
                        ""name"": ""my-app"",
                        ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                        ""space_name"": ""my-space"",
                        ""uris"": [
                            ""my-app.10.244.0.34.xip.io"",
                            ""my-app2.10.244.0.34.xip.io""
                        ],
                        ""users"": null,
                        ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                    }
                }
            }";

            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryApplicationOptions(config);

            Assert.Equal("https://api.system.testcloud.com", options.CF_Api);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.Application_Id);
            Assert.Equal("my-app", options.ApplicationName);
            Assert.Equal("my-app", options.Application_Name);

            Assert.NotNull(options.ApplicationUris);
            Assert.NotNull(options.Application_Uris);
            Assert.Single(options.ApplicationUris);
            Assert.Single(options.Application_Uris);
            Assert.Equal("my-app.10.244.0.34.xip.io", options.ApplicationUris.First());
            Assert.Equal("my-app.10.244.0.34.xip.io", options.Application_Uris.First());

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.ApplicationVersion);
            Assert.Equal("my-app", options.Name);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.SpaceId);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.Space_Id);
            Assert.Equal("my-space", options.SpaceName);
            Assert.Equal("my-space", options.Space_Name);

            Assert.NotNull(options.Uris);
            Assert.Equal(2, options.Uris.Count());
            Assert.Contains("my-app.10.244.0.34.xip.io", options.Uris);
            Assert.Contains("my-app2.10.244.0.34.xip.io", options.Uris);

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }

        [Fact]
        public void ConstructorWithIConfigurationRoot_Binds()
        {
            // Arrange
            var configJson = @"
            {
                ""vcap"": {
                    ""application"" : {
                        ""cf_api"": ""https://api.system.testcloud.com"",
                        ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                        ""application_name"": ""my-app"",
                        ""application_uris"": [
                            ""my-app.10.244.0.34.xip.io""
                        ],
                        ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                        ""limits"": {
                            ""disk"": 1024,
                            ""fds"": 16384,
                            ""mem"": 256
                        },
                        ""name"": ""my-app"",
                        ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                        ""space_name"": ""my-space"",
                        ""uris"": [
                            ""my-app.10.244.0.34.xip.io"",
                            ""my-app2.10.244.0.34.xip.io""
                        ],
                        ""users"": null,
                        ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                    }
                }
            }";

            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryApplicationOptions(config);

            Assert.Equal("https://api.system.testcloud.com", options.CF_Api);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.Application_Id);
            Assert.Equal("my-app", options.ApplicationName);
            Assert.Equal("my-app", options.Application_Name);

            Assert.NotNull(options.ApplicationUris);
            Assert.NotNull(options.Application_Uris);
            Assert.Single(options.ApplicationUris);
            Assert.Single(options.Application_Uris);
            Assert.Equal("my-app.10.244.0.34.xip.io", options.ApplicationUris.First());
            Assert.Equal("my-app.10.244.0.34.xip.io", options.Application_Uris.First());

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.ApplicationVersion);
            Assert.Equal("my-app", options.Name);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.SpaceId);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.Space_Id);
            Assert.Equal("my-space", options.SpaceName);
            Assert.Equal("my-space", options.Space_Name);

            Assert.NotNull(options.Uris);
            Assert.Equal(2, options.Uris.Count());
            Assert.Contains("my-app.10.244.0.34.xip.io", options.Uris);
            Assert.Contains("my-app2.10.244.0.34.xip.io", options.Uris);

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }

        [Fact]
        public void ConstructorWithIConfiguration_Binds()
        {
            // Arrange
            var configJson = @"
            {
                ""vcap"": {
                    ""application"" : {
                        ""cf_api"": ""https://api.system.testcloud.com"",
                        ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                        ""application_name"": ""my-app"",
                        ""application_uris"": [
                            ""my-app.10.244.0.34.xip.io""
                        ],
                        ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                        ""limits"": {
                            ""disk"": 1024,
                            ""fds"": 16384,
                            ""mem"": 256
                        },
                        ""name"": ""my-app"",
                        ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                        ""space_name"": ""my-space"",
                        ""uris"": [
                            ""my-app.10.244.0.34.xip.io"",
                            ""my-app2.10.244.0.34.xip.io""
                        ],
                        ""users"": null,
                        ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                    }
                }
            }";

            var memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
            var jsonSource = new JsonStreamConfigurationSource(memStream);
            var builder = new ConfigurationBuilder().Add(jsonSource);
            var config = builder.Build();

            var options = new CloudFoundryApplicationOptions(config);

            Assert.Equal("https://api.system.testcloud.com", options.CF_Api);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.Application_Id);
            Assert.Equal("my-app", options.ApplicationName);
            Assert.Equal("my-app", options.Application_Name);

            Assert.NotNull(options.ApplicationUris);
            Assert.NotNull(options.Application_Uris);
            Assert.Single(options.ApplicationUris);
            Assert.Single(options.Application_Uris);
            Assert.Equal("my-app.10.244.0.34.xip.io", options.ApplicationUris.First());
            Assert.Equal("my-app.10.244.0.34.xip.io", options.Application_Uris.First());

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.ApplicationVersion);
            Assert.Equal("my-app", options.Name);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.SpaceId);
            Assert.Equal("06450c72-4669-4dc6-8096-45f9777db68a", options.Space_Id);
            Assert.Equal("my-space", options.SpaceName);
            Assert.Equal("my-space", options.Space_Name);

            Assert.NotNull(options.Uris);
            Assert.Equal(2, options.Uris.Count());
            Assert.Contains("my-app.10.244.0.34.xip.io", options.Uris);
            Assert.Contains("my-app2.10.244.0.34.xip.io", options.Uris);

            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }
    }
}
