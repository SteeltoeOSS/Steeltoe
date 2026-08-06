// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteGitPropertiesFallbackFileOverridesGlobalOptOutTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);

        await Workspace.WriteFileAsync(Path.Combine(Workspace.GetPath("repo"), "Directory.Build.props"), """
        <Project>
          <PropertyGroup>
            <GenerateGitProperties>false</GenerateGitProperties>
          </PropertyGroup>
        </Project>
        """);

        TestProject testApp = repository.TestApp;

        await testApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");

        testApp.FallbackGitPropertiesGenerated.Should().BeTrue();
    }
}
