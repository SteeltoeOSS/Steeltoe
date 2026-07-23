// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.AutoDetection;

public sealed class AutoDetectionOverrideDoesNotMatchPackageIdAsPrefixTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        const string shortPackageId = "Some";
        const string longerPackageId = "Some2";

        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        TestProject dependency = await repository.AddDependencyProjectAsync(longerPackageId);
        TestProject testApp = await repository.AddTestAppReferencingAsync(dependency);
        await testApp.BuildAsync($"-p:GitPropertiesConsumingPackageIds={shortPackageId}");
        testApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
