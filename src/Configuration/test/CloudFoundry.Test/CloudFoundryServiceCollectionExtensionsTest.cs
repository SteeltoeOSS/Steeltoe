// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public async Task ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION", """
            {
              "cf_api": "https://api.run.pcfone.io",
              "limits": {
                "fds": 16384
              },
              "application_name": "foo",
              "application_uris": [
                "foo-unexpected-serval-iy.apps.pcfone.io"
              ],
              "name": "foo",
              "space_name": "playground",
              "space_id": "f03f2ab0-cf33-416b-999c-fb01c1247753",
              "organization_id": "d7afe5cb-2d42-487b-a415-f47c0665f1ba",
              "organization_name": "pivot-thess",
              "uris": [
                "foo-unexpected-serval-iy.apps.pcfone.io"
              ],
              "users": null,
              "application_id": "f69a6624-7669-43e3-a3c8-34d23a17e3db"
            }
            """);

        IConfiguration configuration = new ConfigurationBuilder().AddCloudFoundry().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCloudFoundryOptions();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var appOptions = serviceProvider.GetRequiredService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.Equal("foo", appOptions.Value.ApplicationName);
        Assert.Equal(16384, appOptions.Value.Limits?.FileDescriptor);
        Assert.Equal("playground", appOptions.Value.SpaceName);
    }
}
