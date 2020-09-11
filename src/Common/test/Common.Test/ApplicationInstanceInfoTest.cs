// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Steeltoe.Common.Test
{
    public class ApplicationInstanceInfoTest
    {
        [Fact]
        public void ConstructorSetsDefaults()
        {
            var builder = new ConfigurationBuilder();
            var config = builder.Build();

            var options = new ApplicationInstanceInfo(config, true);
            Assert.Null(options.ApplicationId);
            Assert.Equal(Assembly.GetEntryAssembly().GetName().Name, options.ApplicationName);
            Assert.Null(options.ApplicationVersion);
            Assert.Null(options.InstanceId);
            Assert.Equal(-1, options.InstanceIndex);
            Assert.Null(options.Uris);
            Assert.Null(options.Version);
            Assert.Null(options.InstanceIP);
            Assert.Null(options.InternalIP);
            Assert.Equal(-1, options.DiskLimit);
            Assert.Equal(-1, options.FileDescriptorLimit);
            Assert.Equal(-1, options.InstanceIndex);
            Assert.Equal(-1, options.MemoryLimit);
            Assert.Equal(-1, options.Port);
        }

        [Fact]
        public void ConstructorReadsApplicationConfiguration()
        {
            // Arrange
            var configJson = @"
            {
                ""Application"" : {
                    ""Name"": ""my-app"",
                    ""ApplicationId"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                    ""Uris"": [
                        ""my-app.10.244.0.34.xip.io"",
                        ""my-app2.10.244.0.34.xip.io""
                    ],
                    ""Version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                }
            }";

            var path = TestHelpers.CreateTempFile(configJson);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(directory);
            builder.AddJsonFile(fileName);
            var config = builder.Build();

            var options = new ApplicationInstanceInfo(config, true);

            Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
            Assert.Equal("my-app", options.ApplicationName);
            Assert.NotNull(options.Uris);
            Assert.Equal(2, options.Uris.Count());
            Assert.Contains("my-app.10.244.0.34.xip.io", options.Uris);
            Assert.Contains("my-app2.10.244.0.34.xip.io", options.Uris);
            Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
        }
    }
}
