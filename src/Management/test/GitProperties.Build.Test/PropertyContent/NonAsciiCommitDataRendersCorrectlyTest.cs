// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test.PropertyContent;

public sealed class NonAsciiCommitDataRendersCorrectlyTest : GitPropertiesBuildTestBase
{
    [Fact]
    public async Task Test()
    {
        const string nonAsciiUserName = "\u00DCn\u00EFc\u00F6d\u00E9 T\u00EBst";
        const string nonAsciiCommitSubject = "\u00DCn\u00EFc\u00F6d\u00E9 t\u00EBst commit \u65E5\u672C\u8A9E";
        const string commitBody = "Adds a null check before calling Ping().";

        EmptyGitRepository emptyRepository = await Workspace.CreateEmptyRepositoryAsync("repo");
        await emptyRepository.RunGitAsync("config", "user.name", nonAsciiUserName);
        await emptyRepository.RunGitAsync("config", "user.email", "test@example.com");
        await Workspace.WriteFileAsync(Path.Combine(emptyRepository.RootDirectory, ".gitignore"), "bin/\r\nobj/\r\n");
        await Workspace.WriteFileAsync(Path.Combine(emptyRepository.RootDirectory, "file.txt"), "content");
        await emptyRepository.CommitAllAsync(nonAsciiCommitSubject, commitBody);

        GitRepository repository = await emptyRepository.AddTestAppAsync();
        await repository.TestApp.BuildAsync();
        Dictionary<string, string> properties = await repository.TestApp.ReadDebugPropertiesAsync();
        properties["git.commit.user.name"].Should().Be(nonAsciiUserName);
        properties["git.commit.message.short"].Should().Be(nonAsciiCommitSubject);
        properties["git.commit.message.full"].Should().Be($@"{nonAsciiCommitSubject}\n\n{commitBody}");
    }
}
