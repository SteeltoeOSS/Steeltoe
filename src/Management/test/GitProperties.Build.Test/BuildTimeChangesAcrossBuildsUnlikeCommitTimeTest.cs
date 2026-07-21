// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class BuildTimeChangesAcrossBuildsUnlikeCommitTimeTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesBefore = await repository.TestApp.ReadDebugPropertiesAsync();

        await Task.Delay(TimeSpan.FromMilliseconds(1100), TestContext.Current.CancellationToken);
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> propertiesAfter = await repository.TestApp.ReadDebugPropertiesAsync();

        propertiesAfter["git.build.time"].Should().NotBe(propertiesBefore["git.build.time"]);
        propertiesAfter["git.commit.time"].Should().Be(propertiesBefore["git.commit.time"]);
        propertiesAfter["git.commit.id"].Should().Be(propertiesBefore["git.commit.id"]);
    }
}
