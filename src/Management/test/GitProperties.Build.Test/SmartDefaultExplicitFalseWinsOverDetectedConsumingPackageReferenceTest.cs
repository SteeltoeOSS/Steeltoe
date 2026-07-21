// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class SmartDefaultExplicitFalseWinsOverDetectedConsumingPackageReferenceTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync("Steeltoe.Management.Endpoint");
        TestProject testApp = await repository.AddTestAppReferencingAsync(dependency);

        await testApp.BuildAsync("-p:GenerateGitProperties=false");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
