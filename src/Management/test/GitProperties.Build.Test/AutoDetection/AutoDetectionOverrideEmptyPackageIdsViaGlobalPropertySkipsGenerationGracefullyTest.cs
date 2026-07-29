// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.AutoDetection;

public sealed class AutoDetectionOverrideEmptyPackageIdsViaGlobalPropertySkipsGenerationGracefullyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync("Steeltoe.Management.Endpoint");
        TestProject consumingApp = await repository.AddTestAppReferencingAsync(dependency);
        await consumingApp.BuildAsync("-p:GitPropertiesConsumingPackageIds=");
        consumingApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
