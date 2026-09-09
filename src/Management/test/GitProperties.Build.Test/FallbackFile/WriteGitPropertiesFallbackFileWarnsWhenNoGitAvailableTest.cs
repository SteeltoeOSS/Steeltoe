// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteGitPropertiesFallbackFileWarnsWhenNoGitAvailableTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        TestProject testApp = await Workspace.CreateProjectWithoutGitAsync("test-project");
        await Workspace.WriteFileAsync(testApp.FallbackFilePath, ["git.commit.id=deadbeefdeadbeefdeadbeefdeadbeefdeadbeef"]);

        DotNetCommandOutput output = await testApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");
        output.Should().ContainOnlyGitWarning(GitDiagnostic.GitRepositoryNotFound);

        Dictionary<string, string> properties = await testApp.ReadFallbackPropertiesAsync();
        properties["git.commit.id"].Should().Be("deadbeefdeadbeefdeadbeefdeadbeefdeadbeef");
    }
}
