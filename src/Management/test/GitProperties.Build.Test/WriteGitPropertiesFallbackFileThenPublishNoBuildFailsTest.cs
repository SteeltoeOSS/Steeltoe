// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileThenPublishNoBuildFailsTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// Documents/guards the one real caveat called out in PackageReadme.md: this target never produces real build output, so a local "dotnet publish
    /// --no-build" afterward must fail - there is nothing compiled to publish. If this target's own implementation ever accidentally started producing
    /// compiled output (defeating its "lightweight" purpose), this test would start failing for the opposite reason (publish --no-build would start
    /// succeeding) - a signal to revisit the target, not just delete this test.
    /// </summary>
    [Fact]
    public async Task Test()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        await repository.TestApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");

        // Confirms the actual premise of this test: nothing was compiled, so there is genuinely nothing for
        // "dotnet publish --no-build" below to publish.
        repository.TestApp.CompiledAssemblyExists.Should().BeFalse();

        // 1, not just "nonzero": MSBuild's own long-standing, stable convention for "the build failed" (verified
        // against a real "dotnet publish --no-build" in this exact no-compiled-output scenario) - checked here,
        // at the point of the call, rather than via a separate assertion afterward.
        await repository.TestApp.PublishAsync(1, "--no-build");
    }
}
