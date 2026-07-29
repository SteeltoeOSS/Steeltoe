// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.AutoDetection;

public sealed class AutoDetectionSkipsGenerationWhenNoConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        DotNetCommandOutput output = await repository.TestApp.BuildAsync("-v:normal", "-p:GenerateGitProperties=auto");
        output.Value.Should().Contain("git.properties generation skipped: no reference to");
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
