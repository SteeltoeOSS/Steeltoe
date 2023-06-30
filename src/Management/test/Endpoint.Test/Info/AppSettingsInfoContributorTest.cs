// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Info;

public class AppSettingsInfoContributorTest : BaseTest
{
    private readonly Dictionary<string, string> _appSettings = new()
    {
        ["info:application:name"] = "foobar",
        ["info:application:version"] = "1.0.0",
        ["info:application:date"] = "5/1/2008",
        ["info:application:time"] = "8:30:52 AM",
        ["info:NET:type"] = "Core",
        ["info:NET:version"] = "2.0.0"
    };

    [Fact]
    public void ContributeWithConfigNull()
    {
        var contributor = new AppSettingsInfoContributor(null);
        var builder = new InfoBuilder();
        contributor.ContributeAsync(builder);
        Dictionary<string, object> result = builder.Build();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ContributeWithNullBuilderThrows()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        var settings = new AppSettingsInfoContributor(configurationRoot);

        Assert.ThrowsAsync<ArgumentNullException>(() => settings.ContributeAsync(null));
    }

    [Fact]
    public void ContributeAddsToBuilder()
    {
        _appSettings.Add("info:NET:ASPNET:type", "Core");
        _appSettings.Add("info:NET:ASPNET:version", "2.0.0");
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        var settings = new AppSettingsInfoContributor(configurationRoot);

        var builder = new InfoBuilder();
        settings.ContributeAsync(builder);

        Dictionary<string, object> info = builder.Build();
        Assert.NotNull(info);
        Assert.Equal(2, info.Count);
        Assert.True(info.ContainsKey("application"));
        Assert.True(info.ContainsKey("NET"));

        var appNode = info["application"] as Dictionary<string, object>;
        Assert.NotNull(appNode);
        Assert.Equal("foobar", appNode["name"]);

        var netNode = info["NET"] as Dictionary<string, object>;
        Assert.NotNull(netNode);
        Assert.Equal("Core", netNode["type"]);

        Assert.NotNull(netNode["ASPNET"]);
    }
}
