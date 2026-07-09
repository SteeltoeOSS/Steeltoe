// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Management.GitProperties.Build;

internal static class GitPropertiesFileWriter
{
    /// <summary>
    /// The git.properties key that <see cref="GenerateGitPropertiesCacheTask" /> writes and <see cref="ComposeGitPropertiesTask" /> later searches for (to
    /// append the "-dirty" suffix) - shared so the two can never silently drift out of sync with each other.
    /// </summary>
    public const string CommitIdDescribeKey = "git.commit.id.describe";

    /// <summary>
    /// Collapses real newlines to a literal "\n" so a value can never span multiple physical lines (Steeltoe's GitInfoContributor reads this file with
    /// File.ReadAllLinesAsync and would silently truncate a value at the first embedded newline otherwise). Colons are deliberately left unescaped -
    /// Steeltoe only unescapes "\:" back to ":", so leaving real colons alone (timestamps, URLs) is what round-trips correctly.
    /// </summary>
    public static string EscapeLineBreaks(string? value)
    {
        return value is null or "" ? string.Empty : value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\\n");
    }

    /// <summary>
    /// Writes to a per-process temp file then atomically moves it into place, matching the pattern MSBuild's own WriteLinesToFile task uses (since change
    /// wave 18.3) to avoid a concurrent reader ever observing a torn/partial write when multiple projects in a solution build race to populate the same
    /// shared cache file.
    /// </summary>
    public static void WriteAtomic(string path, List<string> lines)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetRandomFileName()}~");
        var encoding = new UTF8Encoding(false);
        File.WriteAllText(tempPath, $"{string.Join("\n", lines)}\n", encoding);

        Exception? lastError = null;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
                return;
            }
            catch (IOException exception)
            {
                lastError = exception;
                Thread.Sleep(10);
            }
        }

        throw new IOException($"Failed to write {path}", lastError);
    }

    /// <summary>
    /// Attempts to acquire exclusive access to <paramref name="lockFilePath" /> (creating it if needed) as a cross-process mutual-exclusion lock, retrying
    /// for up to <paramref name="timeout" /> before giving up. Returns null on timeout, or if locking isn't possible at all in the current environment -
    /// locking here is purely an optimization (avoiding redundant work when multiple projects/TFMs race to populate the same shared cache file), never a
    /// correctness requirement, so any failure to acquire it must fall back to proceeding without one rather than failing the build.
    /// </summary>
    /// <remarks>
    /// Deliberately not a named <see cref="System.Threading.Mutex" />: cross-process named Mutex/Semaphore support on Unix was added later than on Windows
    /// and still carries real caveats (permissions, abandoned-lock semantics). Exclusive file access is a simpler, more universally portable primitive -
    /// it's been supported identically on every OS .NET runs on since .NET Core 1.0 (implemented via ordinary POSIX advisory locking on Unix, not an
    /// OS-specific IPC primitive), and an "abandoned" lock needs no special handling: if the holding process crashes, the OS releases the file handle - and
    /// the lock - automatically on process exit.
    /// </remarks>
    public static FileStream? TryAcquireExclusiveLock(string lockFilePath, TimeSpan timeout)
    {
        string? directory = Path.GetDirectoryName(lockFilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // S6354 (use an injectable time provider): not practical here - System.TimeProvider isn't
        // available on netstandard2.0 without adding a package dependency purely for one retry-loop
        // deadline in a synchronous MSBuild task assembly that has no other testability/DI pattern.
#pragma warning disable S6354
        DateTime deadlineUtc = DateTime.UtcNow + timeout;
#pragma warning restore S6354

        while (true)
        {
            try
            {
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
#pragma warning disable S6354
                if (DateTime.UtcNow >= deadlineUtc)
#pragma warning restore S6354
                {
                    return null;
                }

                Thread.Sleep(50);
            }
        }
    }
}
