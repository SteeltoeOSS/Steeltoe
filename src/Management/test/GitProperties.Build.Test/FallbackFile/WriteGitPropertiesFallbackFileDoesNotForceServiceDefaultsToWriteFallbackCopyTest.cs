// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.FallbackFile;

public sealed class WriteGitPropertiesFallbackFileDoesNotForceServiceDefaultsToWriteFallbackCopyTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);

        TestProject serviceDefaults = await repository.AddTestLibraryAsync("ServiceDefaults", true, [
            Workspace.FakeEndpointPackageReference,
            Workspace.GetGitPropertiesPackageReferenceWithPrivateAssets("none")
        ]);

        TestProject apiService = await repository.AddTestAppAsync("ApiService", projectReferences: [serviceDefaults]);
        await apiService.BuildAsync("-t:WriteGitPropertiesFallbackFile");

        apiService.FallbackGitPropertiesGenerated.Should().BeTrue();
        serviceDefaults.GitPropertiesGenerated.Should().BeTrue();
        serviceDefaults.FallbackGitPropertiesGenerated.Should().BeFalse();
    }
}
