// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

public sealed class WriteGitPropertiesFallbackFileProducesFallbackFileWithoutCompilingTest : GitPropertiesBuildTestBase
{
    /// <summary>
    /// The stable, documented entry point for step 1 of the "Recommended cf push workflow" (see PackageReadme.md) - confirms it actually produces a usable
    /// fallback file, and that doing so never compiles anything (the whole reason to prefer it over a full "dotnet build" before a source push).
    /// </summary>
    [Fact]
    public async Task WriteGitPropertiesFallbackFile_ProducesFallbackFile_WithoutCompiling()
    {
        GitRepository repository = await Workspace.CreateGitRepositoryAsync("repo", 1, true);
        await repository.TestApp.BuildAsync("-t:WriteGitPropertiesFallbackFile");
        repository.TestApp.FallbackGitPropertiesGenerated.Should().BeTrue("the fallback file should have been written next to the .csproj.");

        Dictionary<string, string> properties = await repository.TestApp.ReadFallbackPropertiesAsync();
        string expectedCommitId = await repository.GetCommitIdAsync();
        properties["git.commit.id"].Should().Be(expectedCommitId);

        // "bin\Debug\<TFM>\publish" gets created as empty, routine SDK scaffolding even here (PrepareForPublish's own setup) - checking for the absence
        // of the compiled assembly itself, not just the bin directory, is what actually proves no compilation happened.
        repository.TestApp.CompiledAssemblyExists.Should().BeFalse(
            "this target must never compile the project - that's the whole point of using it instead of a full build before a source push.");
    }
}
