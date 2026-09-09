// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteGitPropertiesFallbackFileWrapperDoesNotForceServiceDefaultsToWriteFallbackCopyTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);

        await Workspace.WriteFileAsync(Path.Combine(Workspace.GetPath("repo"), "Directory.Build.targets"), """
        <Project>
          <Target Name="RefreshGitPropertiesFallbackFile">
            <CallTarget Targets="WriteGitPropertiesFallbackFile" Condition="'$(GitExecutable)' != ''" />
          </Target>
        </Project>
        """);

        TestProject serviceDefaults = await repository.AddTestLibraryAsync("ServiceDefaults", false, [
            Workspace.FakeEndpointPackageReference,
            Workspace.GetGitPropertiesPackageReferenceWithPrivateAssets("none")
        ]);

        await serviceDefaults.BuildAsync("-t:RefreshGitPropertiesFallbackFile");
        serviceDefaults.GitPropertiesGenerated.Should().BeFalse();
        serviceDefaults.FallbackGitPropertiesGenerated.Should().BeFalse();

        TestProject apiService = await repository.AddTestAppAsync("ApiService", projectReferences: [serviceDefaults]);
        await apiService.BuildAsync("-t:RefreshGitPropertiesFallbackFile");
        apiService.GitPropertiesGenerated.Should().BeFalse();
        apiService.FallbackGitPropertiesGenerated.Should().BeTrue();
        serviceDefaults.GitPropertiesGenerated.Should().BeFalse();
        serviceDefaults.FallbackGitPropertiesGenerated.Should().BeFalse();

        TestProject appHost = await repository.AddTestAppAsync("AppHost");
        await appHost.BuildAsync("-t:RefreshGitPropertiesFallbackFile");
        appHost.GitPropertiesGenerated.Should().BeFalse();
        appHost.FallbackGitPropertiesGenerated.Should().BeFalse();
    }
}
