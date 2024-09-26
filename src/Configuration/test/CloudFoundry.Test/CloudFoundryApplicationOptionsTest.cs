// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryApplicationOptionsTest
{
    [Fact]
    public void DefaultConstructor()
    {
        var options = new CloudFoundryApplicationOptions();

        options.ApplicationId.Should().BeNull();
        options.ApplicationName.Should().BeNull();
        options.Uris.Should().BeEmpty();
        options.ApplicationVersion.Should().BeNull();
        options.Api.Should().BeNull();
        options.Limits.Should().BeNull();
        options.OrganizationId.Should().BeNull();
        options.OrganizationName.Should().BeNull();
        options.ProcessId.Should().BeNull();
        options.ProcessType.Should().BeNull();
        options.SpaceId.Should().BeNull();
        options.SpaceName.Should().BeNull();
        options.Start.Should().BeNull();
        options.StartedAtTimestamp.Should().Be(0);
        options.InstanceId.Should().BeNull();
        options.InstanceIndex.Should().Be(-1);
        options.InstancePort.Should().Be(-1);
        options.InstanceIP.Should().BeNull();
        options.InternalIP.Should().BeNull();
    }

    [Fact]
    public void NoVcapApplicationConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        var defaultConfigurer = new ConfigureApplicationInstanceInfo(configuration);
        var cloudFoundryConfigurer = new ConfigureCloudFoundryApplicationOptions(configuration, defaultConfigurer);

        var options = new CloudFoundryApplicationOptions();
        cloudFoundryConfigurer.Configure(options);

        options.ApplicationId.Should().BeNull();
        options.ApplicationName.Should().Be(Assembly.GetEntryAssembly()!.GetName().Name);
        options.Uris.Should().BeEmpty();
        options.ApplicationVersion.Should().BeNull();
        options.Api.Should().BeNull();
        options.Limits.Should().BeNull();
        options.OrganizationId.Should().BeNull();
        options.OrganizationName.Should().BeNull();
        options.ProcessId.Should().BeNull();
        options.ProcessType.Should().BeNull();
        options.SpaceId.Should().BeNull();
        options.SpaceName.Should().BeNull();
        options.Start.Should().BeNull();
        options.StartedAtTimestamp.Should().Be(0);
        options.InstanceId.Should().BeNull();
        options.InstanceIndex.Should().Be(-1);
        options.InstancePort.Should().Be(-1);
        options.InstanceIP.Should().BeNull();
        options.InternalIP.Should().BeNull();
    }

    [Fact]
    public void WithVcapApplicationConfiguration()
    {
        const string vcapApplicationJson = """
            {
                "cf_api": "https://api.system.test-cloud.com",
                "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                "application_name": "my-app",
                "application_uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                ],
                "application_version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca",
                "limits": {
                    "disk": 1024,
                    "fds": 16384,
                    "mem": 256
                },
                "name": "my-app",
                "space_id": "06450c72-4669-4dc6-8096-45f9777db68a",
                "space_name": "my-space",
                "uris": [
                    "my-app.10.244.0.34.xip.io",
                    "my-app2.10.244.0.34.xip.io"
                ],
                "users": null,
                "version": "fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplicationJson);
        using var idScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "some-id");
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "2");
        using var portScope = new EnvironmentVariableScope("CF_INSTANCE_PORT", "8765");
        using var ipScope = new EnvironmentVariableScope("CF_INSTANCE_IP", "1.2.3.4");
        using var internalIpScope = new EnvironmentVariableScope("CF_INSTANCE_INTERNAL_IP", "5.6.7.8");

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfiguration configuration = builder.Build();

        var defaultConfigurer = new ConfigureApplicationInstanceInfo(configuration);
        var cloudFoundryConfigurer = new ConfigureCloudFoundryApplicationOptions(configuration, defaultConfigurer);

        var options = new CloudFoundryApplicationOptions();
        cloudFoundryConfigurer.Configure(options);

        options.ApplicationId.Should().Be("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3");
        options.ApplicationName.Should().Be("my-app");
        options.Uris.Should().HaveCount(2);
        options.Uris[0].Should().Be("my-app.10.244.0.34.xip.io");
        options.Uris[1].Should().Be("my-app2.10.244.0.34.xip.io");
        options.ApplicationVersion.Should().Be("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca");
        options.Api.Should().Be("https://api.system.test-cloud.com");
        options.Limits.Should().NotBeNull();
        options.Limits!.Disk.Should().Be(1024);
        options.Limits!.FileDescriptor.Should().Be(16384);
        options.Limits!.Memory.Should().Be(256);
        options.OrganizationId.Should().BeNull();
        options.OrganizationName.Should().BeNull();
        options.ProcessId.Should().BeNull();
        options.ProcessType.Should().BeNull();
        options.SpaceId.Should().Be("06450c72-4669-4dc6-8096-45f9777db68a");
        options.SpaceName.Should().Be("my-space");
        options.Start.Should().BeNull();
        options.StartedAtTimestamp.Should().Be(0);
        options.InstanceId.Should().Be("some-id");
        options.InstanceIndex.Should().Be(2);
        options.InstancePort.Should().Be(8765);
        options.InstanceIP.Should().Be("1.2.3.4");
        options.InternalIP.Should().Be("5.6.7.8");
    }
}
