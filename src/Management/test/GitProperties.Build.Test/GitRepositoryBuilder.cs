// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.GitProperties.Build.Test;

/// <summary>
/// Builds synthetic git repositories with a controlled, minimal commit history for tests, and simulates the one deployment step (`cf push`) that strips
/// ".git" from what a running app actually sees on disk. Deliberately never operates on a clone of this (large, real) repository, so the suite stays
/// fast - see <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" /> for the workspace-level entry point that also copies in the project
/// files under test.
/// </summary>
internal static class GitRepositoryBuilder
{
    private static readonly HashSet<string> SimulatedPushExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "bin",
        "obj"
    };

    /// <summary>
    /// `git init` only - no config/.gitignore/history. Shared by <see cref="InitializeAsync" /> and
    /// <see cref="GitPropertiesTestWorkspace.CreateEmptyRepositoryAsync" />, so the two never drift out of sync with each other.
    /// </summary>
    public static async Task InitializeEmptyAsync(string destination)
    {
        Directory.CreateDirectory(destination);
        await ProcessRunner.RunGitAsync(destination, "init", "--quiet", "--initial-branch=main", ".");
    }

    /// <summary>
    /// `git init` plus a handful of manufactured commits under <paramref name="destination" />. Deliberately stops short of committing anything beyond those
    /// manufactured files - see <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" /> for the project-files-copy and final commit step this
    /// leaves to its caller.
    /// </summary>
    /// <param name="destination">
    /// The directory to initialize the repository in.
    /// </param>
    /// <param name="commitCount">
    /// The number of manufactured commits to create.
    /// </param>
    /// <param name="gitignoreFallbackFile">
    /// Whether to also list "git.properties" in the repository's .gitignore - see <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" /> for the
    /// full explanation.
    /// </param>
    public static async Task InitializeAsync(string destination, int commitCount, bool gitignoreFallbackFile)
    {
        await InitializeEmptyAsync(destination);
        await ProcessRunner.RunGitAsync(destination, "config", "user.name", "Test User");
        await ProcessRunner.RunGitAsync(destination, "config", "user.email", "test@example.com");

        // Without this, dotnet build's own obj/bin output is untracked and git status correctly
        // (but unhelpfully, for these tests) reports the tree as dirty - real projects always
        // gitignore build output, same as this repo does.
        string gitignoreContent = gitignoreFallbackFile
            ? """
            bin/
            obj/
            git.properties
            """
            : """
            bin/
            obj/
            """;

        await File.WriteAllTextAsync(Path.Combine(destination, ".gitignore"), gitignoreContent, TestContext.Current.CancellationToken);

        for (int commitNumber = 1; commitNumber <= commitCount; commitNumber++)
        {
            await File.WriteAllTextAsync(Path.Combine(destination, $"file{commitNumber}.txt"), $"content {commitNumber}",
                TestContext.Current.CancellationToken);

            await CommitAllAsync(destination, $"Commit {commitNumber}");
        }
    }

    /// <summary>
    /// A small "git add -A / git commit" primitive - used by <see cref="GitPropertiesTestWorkspace.CreateGitRepositoryAsync" /> to commit the project files
    /// it copies in after <see cref="InitializeAsync" /> runs, and by <see cref="EmptyGitRepository.CommitAllAsync" /> for a fully custom commit.
    /// <paramref name="body" />, when given, becomes a second "-m" argument - git's own subject/body convention for a single commit, not a second commit.
    /// </summary>
    public static async Task CommitAllAsync(string repositoryDirectory, string subject, string? body = null)
    {
        await ProcessRunner.RunGitAsync(repositoryDirectory, "add", "-A");

        if (body == null)
        {
            await ProcessRunner.RunGitAsync(repositoryDirectory, "commit", "--quiet", "-m", subject);
        }
        else
        {
            await ProcessRunner.RunGitAsync(repositoryDirectory, "commit", "--quiet", "-m", subject, "-m", body);
        }
    }

    /// <summary>
    /// Copies a directory tree to a brand-new location with no ".git" anywhere in its ancestry, simulating what actually reaches a running app when deployed
    /// via `cf push` using the dotnet_core_buildpack from source: ".git" is excluded from the pushed tree unconditionally by `cf push` itself (a CLI-level
    /// default, independent of ".cfignore" and not something the buildpack has any special handling for - verified against both tools' source), which is
    /// exactly why live git.properties generation can never run server-side for that scenario. "bin"/"obj" are also excluded, mirroring the ".cfignore"
    /// hygiene real projects need anyway (both to avoid pushing stale local build output, and because reusing another location's "obj" as-is would confuse
    /// MSBuild's own incremental state, which embeds absolute paths). Anything else - including an already-generated fallback "git.properties" sitting next
    /// to the ".csproj" - is copied as-is, exactly as it would ride along in the real push payload.
    /// </summary>
    public static string SimulateSourcePush(string sourceDirectory, string destinationDirectory)
    {
        CopyDirectoryExcluding(new DirectoryInfo(sourceDirectory), destinationDirectory, SimulatedPushExcludedDirectoryNames);
        return destinationDirectory;
    }

    private static void CopyDirectoryExcluding(DirectoryInfo source, string destination, HashSet<string> excludedDirectoryNames)
    {
        Directory.CreateDirectory(destination);

        foreach (FileInfo file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(destination, file.Name), true);
        }

        foreach (DirectoryInfo subDirectory in source.GetDirectories())
        {
            if (excludedDirectoryNames.Contains(subDirectory.Name))
            {
                continue;
            }

            CopyDirectoryExcluding(subDirectory, Path.Combine(destination, subDirectory.Name), excludedDirectoryNames);
        }
    }
}
