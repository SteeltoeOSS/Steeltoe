// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileThenPublishNoBuildFailsTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        await repository.TestApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");
        repository.TestApp.CompiledAssemblyExists.Should().BeFalse();
        await repository.TestApp.PublishAsync(1, "--no-build");
    }
}
