// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Transitivity;

public sealed class PrivateAssetsControlsPackageReferenceTransitivityTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);

        TestProject defaultLibrary = await repository.AddTestLibraryAsync("ServiceDefaultsDefault", false, [
            Workspace.FakeEndpointPackageReference,
            Workspace.GetGitPropertiesPackageReferenceWithPrivateAssets(null)
        ]);

        TestProject defaultApp = await repository.AddTestAppAsync("AppDefault", projectReferences: [defaultLibrary]);
        await defaultApp.BuildAsync();
        defaultApp.GitPropertiesGenerated.Should().BeTrue();
        defaultLibrary.GitPropertiesGenerated.Should().BeFalse();

        TestProject noneLibrary = await repository.AddTestLibraryAsync("ServiceDefaultsNone", false, [
            Workspace.FakeEndpointPackageReference,
            Workspace.GetGitPropertiesPackageReferenceWithPrivateAssets("none")
        ]);

        TestProject noneApp = await repository.AddTestAppAsync("AppNone", projectReferences: [noneLibrary]);
        await noneApp.BuildAsync();
        noneApp.GitPropertiesGenerated.Should().BeTrue();
        noneLibrary.GitPropertiesGenerated.Should().BeFalse();

        TestProject allLibrary = await repository.AddTestLibraryAsync("ServiceDefaultsAll", false, [
            Workspace.FakeEndpointPackageReference,
            Workspace.GetGitPropertiesPackageReferenceWithPrivateAssets("all")
        ]);

        TestProject allApp = await repository.AddTestAppAsync("AppAll", projectReferences: [allLibrary]);
        await allApp.BuildAsync();
        allApp.GitPropertiesGenerated.Should().BeFalse();
        allLibrary.GitPropertiesGenerated.Should().BeFalse();
    }
}
