// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class FallbackFileIgnoredWhenLiveGitAvailableTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        await Workspace.WriteFileAsync(repository.TestApp.FallbackFilePath, ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"]);
        DotNetCommandOutput output = await repository.TestApp.BuildAsync("-v:normal");
        output.Value.Should().NotContain("using pre-generated fallback file");

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
