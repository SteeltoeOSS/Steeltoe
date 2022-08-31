// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Contributor.Test;

public class GitInfoContributorTest : BaseTest
{
    [Fact]
    public void ReadGitPropertiesMissingPropertiesFile()
    {
        IConfiguration configuration = new GitInfoContributor().ReadGitProperties("foobar");
        Assert.Null(configuration);
    }

    [Fact]
    public void ReadEmptyGitPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}empty.git.properties";
        IConfiguration configuration = new GitInfoContributor().ReadGitProperties(path);
        Assert.Null(configuration);
    }

    [Fact]
    public void ReadMalformedGitPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}garbage.git.properties";
        IConfiguration configuration = new GitInfoContributor().ReadGitProperties(path);
        Assert.NotNull(configuration);
        Assert.Null(configuration["git"]);
    }

    [Fact]
    public void ReadGoodPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}git.properties";
        IConfiguration configuration = new GitInfoContributor().ReadGitProperties(path);
        Assert.NotNull(configuration);
        Assert.Equal("true", configuration["git:dirty"]);

        // Verify `\:` strings get converted if they exist in the dates/URLs
        Assert.Equal("https://github.com/spring-projects/spring-boot.git", configuration["git:remote:origin:url"]);
        Assert.Equal("2017-07-12T12:40:39-0600", configuration["git:build:time"]);
        Assert.Equal("2017-06-08T06:47:02-0600", configuration["git:commit:time"]);
    }

    [Fact]
    public void ContributeWithNullBuilderThrows()
    {
        // Uses git.properties file in test project
        var contrib = new GitInfoContributor();
        Assert.Throws<ArgumentNullException>(() => contrib.Contribute(null));
    }

    [Fact]
    public void ContributeAddsToBuilder()
    {
        // Uses git.properties file in test project
        var contrib = new GitInfoContributor();
        var builder = new InfoBuilder();
        contrib.Contribute(builder);

        Dictionary<string, object> result = builder.Build();
        Assert.NotNull(result);
        var gitDict = result["git"] as Dictionary<string, object>;
        Assert.NotNull(gitDict);
        Assert.Equal(7, gitDict.Count);
        Assert.True(gitDict.ContainsKey("build"));
        Assert.True(gitDict.ContainsKey("branch"));
        Assert.True(gitDict.ContainsKey("commit"));
        Assert.True(gitDict.ContainsKey("closest"));
        Assert.True(gitDict.ContainsKey("dirty"));
        Assert.True(gitDict.ContainsKey("remote"));
        Assert.True(gitDict.ContainsKey("tags"));

        var gitBuildDict = gitDict["build"] as Dictionary<string, object>;
        Assert.NotNull(gitBuildDict);
        Assert.True(gitBuildDict.ContainsKey("time"));

        // Verify that datetime values are normalized correctly
        object gitBuildTime = gitBuildDict["time"];
        Assert.Equal(DateTime.Parse("2017-07-12T18:40:39Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), gitBuildTime);

        var gitCommitDict = gitDict["commit"] as Dictionary<string, object>;
        Assert.NotNull(gitCommitDict);
        Assert.True(gitCommitDict.ContainsKey("time"));

        // Verify that datetime values are normalized correctly
        object gitCommitTime = gitCommitDict["time"];
        Assert.Equal(DateTime.Parse("2017-06-08T12:47:02Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), gitCommitTime);
    }
}
