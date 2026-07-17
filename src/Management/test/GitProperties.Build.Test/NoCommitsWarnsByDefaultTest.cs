// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class NoCommitsWarnsByDefaultTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// A freshly-initialized repository (real ".git", so GITPROPS001/002 don't fire instead) with zero commits yet - "git rev-parse HEAD" itself fails in
    /// this state, which GenerateGitPropertiesCacheTask.Preflight treats as a routine, forgivable precondition rather than an unexpected failure.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        EmptyGitRepository emptyRepository = await Workspace.CreateEmptyRepositoryAsync("repo");
        GitRepository repository = await emptyRepository.AddTestAppAsync();

        string defaultResult = await repository.TestApp.BuildAsync();
        defaultResult.AssertWarned("GITPROPS005");

        string enableWarningsFalseResult = await repository.TestApp.BuildAsync("-p:GitPropertiesEnableWarnings=false", "-v:normal");
        enableWarningsFalseResult.AssertReportedAsInfoOnly("GITPROPS005", "no commits yet");

        string featureOffResult = await repository.TestApp.BuildAsync("-p:GenerateGitProperties=false");
        featureOffResult.Should().NotContain("GITPROPS005");

        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
