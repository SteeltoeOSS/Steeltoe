// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddCloudFoundry_AddsCloudFoundrySourceToSourcesList()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry();

        configurationBuilder.EnumerateSources<CloudFoundryConfigurationSource>().Should().ContainSingle();
    }

    [Fact]
    public void AddCloudFoundry_CanReadSettingsFromMemory()
    {
        var reader = new CloudFoundryMemorySettingsReader
        {
            ApplicationJson = """
                {
                    "application_id": "fa05c1a9-0fc1-4fbd-bae1-139850dec7a3",
                    "application_name": "my-app",
                    "application_uris": ["my-app.10.244.0.34.xip.io"],
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
                """,
            ServicesJson = """
                {
                    "elephantsql": [{
                        "name": "elephantsql-c6c60",
                        "label": "elephantsql",
                        "tags": [
                            "postgres",
                            "postgresql",
                            "relational"
                        ],
                        "plan": "turtle",
                        "credentials": {"uri": "postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd"}
                    }],
                    "sendgrid": [
                    {
                        "name": "mysendgrid",
                        "label": "sendgrid",
                        "tags": ["smtp"],
                        "plan": "free",
                        "credentials": {
                            "hostname": "smtp.sendgrid.net",
                            "username": "QvsXMbJ3rK",
                            "password": "HCHMOYluTv"
                        }
                    }]
                }
                """,
            InstanceId = "7c19d892-21c2-496b-a42a-946bbaa0775e",
            InstanceIndex = "0",
            InstanceInternalIP = "127.0.0.1",
            InstanceIP = "10.41.1.1",
            InstancePort = "8888"
        };

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry(reader);
        using ConfigurationRoot configurationRoot = builder.BuildAsRoot();

        configurationRoot["vcap:application:application_id"].Should().Be("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3");
        configurationRoot["vcap:application:limits:disk"].Should().Be("1024");
        configurationRoot["vcap:application:uris:0"].Should().Be("my-app.10.244.0.34.xip.io");
        configurationRoot["vcap:application:uris:1"].Should().Be("my-app2.10.244.0.34.xip.io");
        configurationRoot["vcap:services:elephantsql:0:name"].Should().Be("elephantsql-c6c60");
        configurationRoot["vcap:services:sendgrid:0:name"].Should().Be("mysendgrid");

        configurationRoot["vcap:application:instance_id"].Should().Be("7c19d892-21c2-496b-a42a-946bbaa0775e");
        configurationRoot["vcap:application:instance_index"].Should().Be("0");
        configurationRoot["vcap:application:internal_ip"].Should().Be("127.0.0.1");
        configurationRoot["vcap:application:instance_ip"].Should().Be("10.41.1.1");
        configurationRoot["vcap:application:port"].Should().Be("8888");
    }
}
