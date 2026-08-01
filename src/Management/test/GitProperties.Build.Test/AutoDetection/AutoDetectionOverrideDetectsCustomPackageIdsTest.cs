// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.AutoDetection;

public sealed class AutoDetectionOverrideDetectsCustomPackageIdsTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        const string customPackageId = "Example.Package.Name";

        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddTestLibraryAsync(customPackageId);
        TestProject consumingApp = await repository.AddTestAppAsync("ConsumerApp", [Workspace.GitPropertiesPackageReference], [dependency]);
        await consumingApp.BuildAsync($"-p:GitPropertiesConsumingPackageIds={customPackageId}");
        Dictionary<string, string> properties = await consumingApp.ReadDebugPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);
    }
}
