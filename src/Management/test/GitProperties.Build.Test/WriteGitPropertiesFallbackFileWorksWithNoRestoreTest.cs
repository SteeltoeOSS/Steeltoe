// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileWorksWithNoRestoreTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// "--no-restore" must work the same way for this target as for any other build invocation - it only requires that restore already happened at least
    /// once, same as a normal build.
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_WorksWithNoRestore()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        await repository.TestApp.RestoreAsync();
        await repository.TestApp.BuildAsync("--no-restore", "-t:WriteGitPropertiesFallbackFile");
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue();
    }
}
