// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Computes the git.properties fields that are stable across the whole repository and writes them to a shared cache file, so concurrent and/or
/// multi-targeted projects in the same solution build reuse it instead of each re-invoking git.
/// </summary>
// ReSharper disable once UnusedType.Global
public sealed class GenerateGitPropertiesCacheTask : Task
{
    private const string VersionCheckArguments = "--version";

    // Git requires at least 4 hex digits to keep an abbreviated commit ID meaningfully unique, and no more than a full SHA-1 (40) or SHA-256 (64) hash.
    private const int MinimumCommitIdAbbrevLength = 4;
    private const int MaximumCommitIdAbbrevLength = 64;

    private static readonly Version MinimumGitVersion = new(2, 15, 0);

    /// <summary>
    /// How long to wait for the cross-process cache lock before generating the cache anyway. Sized to comfortably cover a real, if slow, cache generation
    /// without blocking every other concurrently-building project/TFM too long if the holder is actually stuck rather than just slow.
    /// </summary>
    private static readonly TimeSpan CacheLockTimeout = TimeSpan.FromSeconds(10);

    private static readonly char[] CommitLogFieldSeparator = [(char)0x1F];

    private static readonly char[] LineSeparators =
    [
        '\r',
        '\n'
    ];

    // Ordered so a more specific/reliable variable for the same CI provider is tried before a less reliable one.
    private static readonly string[] BranchEnvironmentVariableNames =
    [
        "GITHUB_HEAD_REF",
        "GITHUB_REF_NAME",
        "BUILD_SOURCEBRANCH",
        "CI_MERGE_REQUEST_SOURCE_BRANCH_NAME",
        "CI_COMMIT_REF_NAME",
        "GIT_LOCAL_BRANCH",
        "BRANCH_NAME",
        "GIT_BRANCH",
        "CIRCLE_BRANCH",
        "TRAVIS_BRANCH",
        "APPVEYOR_REPO_BRANCH",
        "BITBUCKET_BRANCH",
        "BUILDKITE_BRANCH",
        "DRONE_BRANCH",
        "SEMAPHORE_GIT_BRANCH",
        "CIRRUS_BRANCH"
    ];

    private bool IsCommitIdAbbrevLengthValid =>
        int.TryParse(CommitIdAbbrevLength, NumberStyles.None, CultureInfo.InvariantCulture, out int length) &&
        length is >= MinimumCommitIdAbbrevLength and <= MaximumCommitIdAbbrevLength;

    /// <summary>
    /// Gets or sets the resolved git repository root directory.
    /// </summary>
    [Required]
    public string RepositoryRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the git executable to invoke.
    /// </summary>
    [Required]
    public string GitExecutable { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shared cache file to write.
    /// </summary>
    [Required]
    public string CacheFile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the length of the abbreviated commit ID to generate.
    /// </summary>
    [Required]
    public string CommitIdAbbrevLength { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether a forgivable anomaly is reported as a warning (true) or an informational message (false).
    /// </summary>
    [Required]
    public bool EnableWarnings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to report when the shared cache is (re)generated.
    /// </summary>
    [Required]
    public bool ReportFileWrites { get; set; }

    /// <inheritdoc />
    public override bool Execute()
    {
        if (!IsCommitIdAbbrevLengthValid)
        {
            Log.LogError(
                $"git.properties: 'GitCommitIdAbbrevLength' must be an integer between {MinimumCommitIdAbbrevLength} and {MaximumCommitIdAbbrevLength}.");

            return false;
        }

        GitVersionStatus versionStatus = CheckGitVersion();

        return versionStatus switch
        {
            GitVersionStatus.Unknown => false,
            GitVersionStatus.Incompatible => true,
            _ => this.LogOnFailure("an unexpected error occurred while generating the shared cache", () =>
            {
                string? commitId = TryGetGitCommitId();

                if (commitId == null)
                {
                    return true;
                }

                return TryGenerate(commitId);
            })
        };
    }

    private GitVersionStatus CheckGitVersion()
    {
        string? output = GetGitVersion();

        if (output != null)
        {
            Version? installedVersion = GitOutputParser.ParseGitVersion(output);

            if (installedVersion != null)
            {
                if (installedVersion >= MinimumGitVersion)
                {
                    return GitVersionStatus.Compatible;
                }

                GitDiagnosticReporter.Report(Log, GitDiagnosticId.IncompatibleGitVersion, EnableWarnings,
                    $"git.properties generation skipped: installed git version {installedVersion} is older than the minimum supported version ({MinimumGitVersion}). " +
                    $"Upgrade git to resolve this.");
            }
            else
            {
                Log.LogError($"git.properties: could not parse the installed git version from '{GitExecutable} {VersionCheckArguments}' output: '{output}'.");
                return GitVersionStatus.Unknown;
            }
        }

        return GitVersionStatus.Incompatible;
    }

    private string? GetGitVersion()
    {
        string output;
        int exitCode;

        try
        {
            exitCode = TryRunGit(VersionCheckArguments, out output, out _);
        }
        catch (Exception exception)
        {
            string message = $"git.properties generation skipped: could not run '{GitExecutable}' ({exception.Message}).";
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitExecutableNotFound, EnableWarnings, message);
            return null;
        }

        if (exitCode != 0)
        {
            string message = $"git.properties generation skipped: '{GitExecutable} {VersionCheckArguments}' exited with code {exitCode}.";
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitExecutableNotFound, EnableWarnings, message);
            return null;
        }

        return output;
    }

    private string? TryGetGitCommitId()
    {
        int exitCode = TryRunGit("rev-parse --is-inside-work-tree", out string stdout, out _);

        if (exitCode != 0 || stdout != "true")
        {
            string message = $"git.properties generation skipped: '{RepositoryRoot}' is not inside a usable git repository.";
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitRepositoryInvalid, EnableWarnings, message);
            return null;
        }

        exitCode = TryRunGit("rev-parse HEAD", out stdout, out _);

        if (exitCode != 0)
        {
            GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitRepositoryHasNoCommits, EnableWarnings,
                "git.properties generation skipped: repository has no commits yet.");

            return null;
        }

        return stdout;
    }

    private bool TryGenerate(string commitId)
    {
        // Wrapped in a cross-process lock: MSBuild's own Inputs/Outputs staleness check runs independently per project/TFM, so multiple projects/TFMs
        // building concurrently can all see the shared cache as stale and invoke this task at once.
        // Only checks whether the file was rewritten by someone else while waiting for the lock. It doesn't check whether the existing content is still
        // correct, which is the staleness check's job. Comparing the cache's stored commit ID instead would be wrong: tagging an existing commit invalidates
        // the cache without changing the commit ID, so that check would silently skip writes that are actually needed. This is purely a "thundering herd"
        // optimization, not a correctness fix. AtomicFile.Write already guarantees no reader ever observes a torn/partial file even without it.

        DateTime? cacheWriteTimeBeforeLock = File.Exists(CacheFile) ? File.GetLastWriteTimeUtc(CacheFile) : null;

        // Never deleted after use, only closed: a concurrent builder could recreate the same path a moment later, and
        // "delete, then someone else recreates it" is the TOCTOU (Time-of-Check to Time-of-Use) race that breaks a file-based mutex.
        using FileStream? cacheLock = AtomicFile.TryAcquireExclusiveLock($"{CacheFile}.lock", CacheLockTimeout);

        if (cacheLock == null)
        {
            Log.LogMessage("git.properties: could not acquire the lock for '{0}' within {1} seconds. Proceeding without it.", CacheFile,
                CacheLockTimeout.TotalSeconds);
        }
        else if (WasCacheRewrittenWhileWaitingForLock(cacheWriteTimeBeforeLock))
        {
            Log.LogMessage(
                "git.properties: shared cache at '{0}' was rewritten by another concurrently-building project or target framework while waiting for the lock. Skipping.",
                CacheFile);

            return true;
        }

        if (ReportFileWrites)
        {
            Log.LogMessage(MessageImportance.High, "git.properties: generating shared cache at '{0}'.", CacheFile);
        }

        if (RunGit("rev-parse --is-shallow-repository", "determine shallow-clone status", out string stdout))
        {
            bool isShallow = stdout == "true";

            if (isShallow)
            {
                GitDiagnosticReporter.Report(Log, GitDiagnosticId.GitRepositoryIsShallowClone, EnableWarnings,
                    "git.properties: repository is a shallow clone. git.total.commit.count and git.closest.tag.commit.count will be left empty. " +
                    "Run 'git fetch --unshallow' to fetch full history, or configure your CI checkout for full depth (e.g. GitHub Actions: fetch-depth: 0).");
            }

            CommitLogEntry? logEntry = GetLatestCommitLogEntry();

            if (logEntry != null)
            {
                TagDescription tagDescription = DescribeClosestTag(isShallow);
                TagsAndCommitCount? tagsAndCommitCount = ReadTagsAndTotalCommitCount(isShallow);

                if (tagsAndCommitCount != null)
                {
                    GitConfig? config = ReadConfig();

                    if (config != null)
                    {
                        string branch = ResolveBranch();
                        string buildHost = Environment.MachineName;

                        List<string> lines =
                        [
                            $"git.branch={GitPropertiesFormat.EscapeLineBreaks(branch)}",
                            $"git.commit.id={GitPropertiesFormat.EscapeLineBreaks(commitId)}",
                            $"git.commit.id.abbrev={GitPropertiesFormat.EscapeLineBreaks(logEntry.AbbrevId)}",
                            $"{GitPropertiesFormat.CommitIdDescribeKey}={GitPropertiesFormat.EscapeLineBreaks(tagDescription.BaseDescribe)}",
                            $"git.commit.time={GitPropertiesFormat.EscapeLineBreaks(logEntry.CommitTime)}",
                            $"git.commit.message.short={GitPropertiesFormat.EscapeLineBreaks(logEntry.ShortMessage)}",
                            $"git.commit.message.full={GitPropertiesFormat.EscapeLineBreaks(logEntry.FullMessage)}",
                            $"git.commit.user.name={GitPropertiesFormat.EscapeLineBreaks(logEntry.AuthorName)}",
                            $"git.commit.user.email={GitPropertiesFormat.EscapeLineBreaks(logEntry.AuthorEmail)}",
                            $"git.build.host={GitPropertiesFormat.EscapeLineBreaks(buildHost)}",
                            $"git.build.user.name={GitPropertiesFormat.EscapeLineBreaks(config.UserName)}",
                            $"git.build.user.email={GitPropertiesFormat.EscapeLineBreaks(config.UserEmail)}",
                            $"git.tags={GitPropertiesFormat.EscapeLineBreaks(tagsAndCommitCount.Tags)}",
                            $"git.closest.tag.name={GitPropertiesFormat.EscapeLineBreaks(tagDescription.ClosestTagName)}",
                            $"git.closest.tag.commit.count={GitPropertiesFormat.EscapeLineBreaks(tagDescription.ClosestTagCommitCount)}",
                            $"git.remote.origin.url={GitPropertiesFormat.EscapeLineBreaks(config.RemoteUrl)}",
                            $"git.total.commit.count={GitPropertiesFormat.EscapeLineBreaks(tagsAndCommitCount.TotalCommitCount)}"
                        ];

                        return this.LogOnFailure($"failed to write {CacheFile}", () =>
                        {
                            AtomicFile.Write(CacheFile, lines);
                        });
                    }
                }
            }
        }

        return false;
    }

    private bool WasCacheRewrittenWhileWaitingForLock(DateTime? writeTimeBeforeLock)
    {
        if (File.Exists(CacheFile))
        {
            return writeTimeBeforeLock == null || File.GetLastWriteTimeUtc(CacheFile) > writeTimeBeforeLock.Value;
        }

        return false;
    }

    private CommitLogEntry? GetLatestCommitLogEntry()
    {
        if (RunGit($"log -1 --abbrev={CommitIdAbbrevLength} --pretty=format:%h%x1f%an%x1f%ae%x1f%cI%x1f%s%x1f%B", "read commit metadata", out string stdout))
        {
            string[] logFields = stdout.Split(CommitLogFieldSeparator, 6);

            return new CommitLogEntry(logFields.Length > 0 ? logFields[0] : string.Empty, logFields.Length > 1 ? logFields[1] : string.Empty,
                logFields.Length > 2 ? logFields[2] : string.Empty, logFields.Length > 3 ? logFields[3] : string.Empty,
                logFields.Length > 4 ? logFields[4] : string.Empty, logFields.Length > 5 ? logFields[5] : string.Empty);
        }

        return null;
    }

    private TagDescription DescribeClosestTag(bool isShallow)
    {
        int exitCode = TryRunGit("describe --tags --long --always", out string stdout, out _);
        TagDescription description = exitCode == 0 ? GitOutputParser.ParseTagDescribe(stdout) : TagDescription.Empty;

        if (isShallow)
        {
            // Ancestry walk is truncated on a shallow clone, so a "count" here would be silently wrong.
            description = new TagDescription(description.BaseDescribe, description.ClosestTagName, string.Empty);
        }

        return description;
    }

    private TagsAndCommitCount? ReadTagsAndTotalCommitCount(bool isShallow)
    {
        if (RunGit("tag --points-at HEAD", "list tags", out string stdout))
        {
            string tags = string.Join(",", stdout.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries));

            if (isShallow)
            {
                return new TagsAndCommitCount(tags, string.Empty);
            }

            if (RunGit("rev-list --count HEAD", "count commits", out stdout))
            {
                return new TagsAndCommitCount(tags, stdout);
            }
        }

        return null;
    }

    private GitConfig? ReadConfig()
    {
        if (RunGit("config --list", "read git config", out string stdout))
        {
            return GitOutputParser.ParseConfig(stdout);
        }

        return null;
    }

    private string ResolveBranch()
    {
        if (RunGit("rev-parse --abbrev-ref HEAD", "determine branch name", out string stdout) && stdout != "HEAD")
        {
            return stdout;
        }

        // Most CI checkouts leave HEAD detached, where git has no branch name to report at all.
        string? branchName = BranchEnvironmentVariableNames.Select(Environment.GetEnvironmentVariable).FirstOrDefault(value => !string.IsNullOrEmpty(value));
        return branchName == null ? string.Empty : NormalizeBranchName(branchName);
    }

    private static string NormalizeBranchName(string value)
    {
        const string refsHeadsPrefix = "refs/heads/";
        const string originPrefix = "origin/";

        if (value.StartsWith(refsHeadsPrefix, StringComparison.Ordinal))
        {
            return value.Substring(refsHeadsPrefix.Length);
        }

        if (value.StartsWith(originPrefix, StringComparison.Ordinal))
        {
            return value.Substring(originPrefix.Length);
        }

        return value;
    }

    private bool RunGit(string arguments, string description, out string stdout)
    {
        int exitCode = TryRunGit(arguments, out stdout, out string stderr);

        if (exitCode == 0)
        {
            return true;
        }

        Log.LogError("git.properties: failed to {0} (exit code {1}): {2}", description, exitCode, stderr);
        return false;
    }

    private int TryRunGit(string arguments, out string stdout, out string stderr)
    {
        return GitProcessRunner.Run(GitExecutable, RepositoryRoot, arguments, out stdout, out stderr);
    }

    /// <summary>
    /// Indicates whether the installed git version can be used.
    /// </summary>
    private enum GitVersionStatus
    {
        /// <summary>
        /// Git ran and satisfies the minimum required version constraint. It is safe to proceed.
        /// </summary>
        Compatible,

        /// <summary>
        /// Git couldn't be run at all or is older than the minimum required version. Both are forgivable, so generation simply skips.
        /// </summary>
        Incompatible,

        /// <summary>
        /// Failed to parse output from "git --version". This isn't a routine, anticipated condition like the other two, so there's no safe fallback. This stops
        /// the build instead of letting a stale cache get used regardless.
        /// </summary>
        Unknown
    }

    private sealed class CommitLogEntry(string abbrevId, string authorName, string authorEmail, string commitTime, string shortMessage, string fullMessage)
    {
        public string AbbrevId { get; } = abbrevId;
        public string AuthorName { get; } = authorName;
        public string AuthorEmail { get; } = authorEmail;
        public string CommitTime { get; } = commitTime;
        public string ShortMessage { get; } = shortMessage;
        public string FullMessage { get; } = fullMessage;
    }

    private sealed class TagsAndCommitCount(string tags, string totalCommitCount)
    {
        public string Tags { get; } = tags;
        public string TotalCommitCount { get; } = totalCommitCount;
    }
}
