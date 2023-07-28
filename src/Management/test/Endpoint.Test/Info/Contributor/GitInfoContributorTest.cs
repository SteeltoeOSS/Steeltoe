// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Info.Contributor;

public sealed class GitInfoContributorTest : BaseTest
{
    [Fact]
    public async Task ReadGitPropertiesMissingPropertiesFile()
    {
        IConfiguration configuration =
            await new GitInfoContributor(NullLogger<GitInfoContributor>.Instance).ReadGitPropertiesAsync("foobar", CancellationToken.None);

        Assert.Null(configuration);
    }

    [Fact]
    public async Task ReadEmptyGitPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}empty.git.properties";

        IConfiguration configuration =
            await new GitInfoContributor(NullLogger<GitInfoContributor>.Instance).ReadGitPropertiesAsync(path, CancellationToken.None);

        Assert.Null(configuration);
    }

    [Fact]
    public async Task ReadMalformedGitPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}garbage.git.properties";

        IConfiguration configuration =
            await new GitInfoContributor(NullLogger<GitInfoContributor>.Instance).ReadGitPropertiesAsync(path, CancellationToken.None);

        Assert.NotNull(configuration);
        Assert.Null(configuration["git"]);
    }

    [Fact]
    public async Task ReadGoodPropertiesFile()
    {
        string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}git.properties";

        IConfiguration configuration =
            await new GitInfoContributor(NullLogger<GitInfoContributor>.Instance).ReadGitPropertiesAsync(path, CancellationToken.None);

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
        var contributor = new GitInfoContributor(NullLogger<GitInfoContributor>.Instance);
        Assert.ThrowsAsync<ArgumentNullException>(() => contributor.ContributeAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task ContributeAddsToBuilder()
    {
        // Uses git.properties file in test project
        var contributor = new GitInfoContributor(NullLogger<GitInfoContributor>.Instance);
        var builder = new InfoBuilder();
        await contributor.ContributeAsync(builder, CancellationToken.None);

        IDictionary<string, object> result = builder.Build();
        Assert.NotNull(result);
        var gitDictionary = result["git"] as Dictionary<string, object>;
        Assert.NotNull(gitDictionary);
        Assert.Equal(7, gitDictionary.Count);
        Assert.True(gitDictionary.ContainsKey("build"));
        Assert.True(gitDictionary.ContainsKey("branch"));
        Assert.True(gitDictionary.ContainsKey("commit"));
        Assert.True(gitDictionary.ContainsKey("closest"));
        Assert.True(gitDictionary.ContainsKey("dirty"));
        Assert.True(gitDictionary.ContainsKey("remote"));
        Assert.True(gitDictionary.ContainsKey("tags"));

        var gitBuildDictionary = gitDictionary["build"] as Dictionary<string, object>;
        Assert.NotNull(gitBuildDictionary);
        Assert.True(gitBuildDictionary.ContainsKey("time"));

        // Verify that datetime values are normalized correctly
        object gitBuildTime = gitBuildDictionary["time"];
        Assert.Equal(DateTime.Parse("2017-07-12T18:40:39Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), gitBuildTime);

        var gitCommitDictionary = gitDictionary["commit"] as Dictionary<string, object>;
        Assert.NotNull(gitCommitDictionary);
        Assert.True(gitCommitDictionary.ContainsKey("time"));

        // Verify that datetime values are normalized correctly
        object gitCommitTime = gitCommitDictionary["time"];
        Assert.Equal(DateTime.Parse("2017-06-08T12:47:02Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal), gitCommitTime);
    }
}
