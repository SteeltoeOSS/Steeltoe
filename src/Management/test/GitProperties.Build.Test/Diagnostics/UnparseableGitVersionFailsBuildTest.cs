// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.Diagnostics;

public sealed class UnparseableGitVersionFailsBuildTest : GitPropertiesTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1);
        string fakeGitExecutable = await Workspace.CreateFakeGitExecutableAsync("invalid-version");

        DotNetCommandOutput output = await repository.TestApp.BuildAsync(1, null, $"-p:GitExecutable={fakeGitExecutable}");
        output.Value.Should().Contain("could not parse the installed git version");
        repository.TestApp.GitPropertiesGenerated.Should().BeFalse();
    }
}
