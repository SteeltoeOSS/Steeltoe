// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test;

public class ManagementEndpointOptionsTest : BaseTest
{
    [Fact]
    public void InitializedWithDefaults()
    {
        var opts = new ManagementEndpointOptions();
        Assert.False(opts.Enabled.HasValue);
        Assert.Equal("/actuator", opts.Path);
    }

    //[Fact]
    //public void ThrowsIfConfigNull()
    //{
    //    const IConfiguration configuration = null;
    //    Assert.Throws<ArgumentNullException>(() => new ManagementEndpointOptions(configuration));
    //}

    //[Fact]
    //public void BindsConfigurationCorrectly()
    //{
    //    var appsettings = new Dictionary<string, string>
    //    {
    //        ["management:endpoints:enabled"] = "false",
    //        ["management:endpoints:path"] = "/management",
    //        ["management:endpoints:info:enabled"] = "true",
    //        ["management:endpoints:info:id"] = "/infomanagement"
    //    };

    //    var configurationBuilder = new ConfigurationBuilder();
    //    configurationBuilder.AddInMemoryCollection(appsettings);
    //    IConfigurationRoot configurationRoot = configurationBuilder.Build();

    //    var opts = new ManagementEndpointOptions(configurationRoot);
    //    Assert.False(opts.Enabled);
    //    Assert.Equal("/management", opts.Path);
    //}

    //[Fact]
    //public void IsExposedCorrectly()
    //{
    //    var managementOptions = new ActuatorManagementOptions
    //    {
    //        Exposure =
    //        {
    //            Exclude = new[]
    //            {
    //                "*"
    //            }.ToList()
    //        }
    //    };

    //    var options = new InfoEndpointOptions();
    //    Assert.False(options.EndpointOptions.IsExposed(managementOptions));
    //}
}
