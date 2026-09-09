// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Submodule;

public sealed class SubmoduleGeneratesOwnGitPropertiesTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository library = await Workspace.CreateGitRepositoryAsync("library", 1);
        await library.TagAsync("lib-v1");
        string libraryCommitId = await library.GetCommitIdAsync();

        GitRepository superProject = await Workspace.CreateGitRepositoryAsync("super", 1);
        GitRepository submoduleLibrary = await superProject.AddSubmoduleAsync("vendor/library", library);

        DotNetCommandOutput output = await submoduleLibrary.TestApp.BuildAsync();
        output.Should().NotContainAnyGitWarnings();
        submoduleLibrary.TestApp.GitPropertiesGenerated.Should().BeTrue();

        Dictionary<string, string> submoduleProperties = await submoduleLibrary.TestApp.ReadDebugPropertiesAsync();
        submoduleProperties["git.commit.id"].Should().Be(libraryCommitId);
        submoduleProperties["git.tags"].Should().Be("lib-v1");

        await superProject.TestApp.BuildAsync();
        Dictionary<string, string> superProperties = await superProject.TestApp.ReadDebugPropertiesAsync();
        superProperties["git.branch"].Should().Be("main");
        superProperties["git.commit.id"].Should().NotBe(submoduleProperties["git.commit.id"]);

        submoduleLibrary.SharedCacheExists.Should().BeTrue();
        superProject.SharedCacheExists.Should().BeTrue();
    }
}
