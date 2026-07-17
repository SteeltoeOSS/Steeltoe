// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class FallbackFileIgnoredWhenLiveGitAvailableTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Guards against a stale fallback file (left over from some earlier build) ever shadowing live generation - the fallback file must only ever be used as
    /// a last resort, never preferred over a real, currently-usable .git repository.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);

        await File.WriteAllLinesAsync(repository.TestApp.FallbackFilePath, ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"],
            TestContext.Current.CancellationToken);

        string result = await repository.TestApp.BuildAsync("-v:detailed");
        result.Should().NotContain("using pre-generated fallback file", "the fallback notice must not appear when live generation actually ran.");

        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
