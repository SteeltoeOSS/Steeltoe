// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryApplicationOptionsTest
{
    [Fact]
    public void Constructor_WithNoVcapApplicationConfiguration()
    {
        var builder = new ConfigurationBuilder();
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryApplicationOptions(configurationRoot);

        Assert.Null(options.CF_Api);
        Assert.Null(options.ApplicationId);
        Assert.Null(options.ApplicationName);
        Assert.Null(options.ApplicationUris);
        Assert.Null(options.Application_Uris);
        Assert.Null(options.ApplicationVersion);
        Assert.Null(options.Application_Version);
        Assert.Null(options.InstanceId);
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
        Assert.Null(options.InstanceIp);
        Assert.Null(options.Internal_IP);
        Assert.Null(options.InternalIp);
        Assert.Equal(-1, options.DiskLimit);
        Assert.Equal(-1, options.FileDescriptorLimit);
        Assert.Equal(-1, options.InstanceIndex);
        Assert.Equal(-1, options.MemoryLimit);
        Assert.Equal(-1, options.Port);
    }

    [Fact]
    public void Constructor_WithVcapApplicationConfiguration()
    {
        const string configJson = @"
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

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();

        var options = new CloudFoundryApplicationOptions(configurationRoot);

        Assert.Equal("https://api.system.testcloud.com", options.CF_Api);
        Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", options.ApplicationId);
        Assert.Equal("my-app", options.ApplicationName);

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
