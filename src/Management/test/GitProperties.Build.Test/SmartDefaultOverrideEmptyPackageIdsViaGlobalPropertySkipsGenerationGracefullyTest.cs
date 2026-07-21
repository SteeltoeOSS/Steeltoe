// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultOverrideEmptyPackageIdsViaGlobalPropertySkipsGenerationGracefullyTest : GitPropertiesBuildTestBase
{
    // Global properties can't be reassigned by the project's own conditional default, so this reaches the task's PackageIds parameter as a genuinely
    // empty string, not "unset". That parameter must NOT be [Required]: MSBuild treats empty the same as "not supplied" and would fail with MSB4044.
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync("Steeltoe.Management.Endpoint");
        TestProject testApp = await repository.AddTestAppReferencingAsync(dependency);
        await testApp.BuildAsync("-p:GitPropertiesConsumingPackageIds=");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
