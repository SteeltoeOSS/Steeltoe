// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Safely reads, writes, and locks files that multiple projects and target frameworks in a solution build may touch at the same time.
/// </summary>
/// <remarks>
/// MSBuild builds multiple projects - and multiple target frameworks of the same project - concurrently by default. When several of those share a single
/// file on disk, two problems follow: a reader can open the file at the exact moment a writer is replacing its content, and two writers can try to
/// update the same file at once. Plain file I/O handles neither case reliably, so every method here is built to tolerate both: writes go through a swap
/// that never leaves the file half-written, reads and writes both retry briefly if they land on the wrong side of someone else's swap, and an optional
/// lock lets concurrent writers avoid redoing the same expensive work instead of every one of them racing to produce the same result.
/// </remarks>
internal static class AtomicFile
{
    /// <summary>
    /// How many times a failed read or write is retried, and how long to wait between attempts.
    /// </summary>
    private const int MaxAttempts = 10;

    private static readonly TimeSpan ReadWriteRetryDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan AcquireLockRetryDelay = TimeSpan.FromMilliseconds(50);

#pragma warning disable S6354 // Use a testable date/time provider
    // Justification: System.TimeProvider isn't available on netstandard2.0.
    private static DateTime CurrentTimeUtc => DateTime.UtcNow;
#pragma warning restore S6354 // Use a testable date/time provider

    /// <summary>
    /// Writes the given lines to <paramref name="path" /> so that anyone reading it - even concurrently - only ever sees the complete previous content or
    /// the complete new content, never a partial write. Retries briefly if another process is momentarily in the way.
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
        Exception? lastError = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                File.WriteAllText(tempPath, $"{string.Join("\n", lines)}\n", encoding);
                MoveOrReplace(tempPath, path);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastError = exception;
                Thread.Sleep(ReadWriteRetryDelay);
            }
        }

        throw new IOException($"Failed to write {path}", lastError);
    }

    /// <summary>
    /// Reads all lines from <paramref name="path" />, retrying briefly if another process is momentarily in the way - the read-side counterpart to
    /// <see cref="WriteAtomic" />.
    /// </summary>
    public static string[] ReadAllLinesWithRetry(string path)
    {
        Exception? lastError = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return File.ReadAllLines(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                lastError = exception;
                Thread.Sleep(ReadWriteRetryDelay);
            }
        }

        throw new IOException($"Failed to read {path}", lastError);
    }

    /// <summary>
    /// Moves a freshly-written file into place, whether or not something is already there.
    /// </summary>
    private static void MoveOrReplace(string sourcePath, string destinationPath)
    {
        try
        {
            File.Move(sourcePath, destinationPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException && File.Exists(destinationPath))
        {
            File.Replace(sourcePath, destinationPath, null);
        }
    }

    /// <summary>
    /// Attempts to become the sole holder of <paramref name="lockFilePath" /> across every process trying to acquire it, for up to
    /// <paramref name="timeout" />. Returns null if that doesn't happen in time, or if locking isn't possible at all in this environment - holding this lock
    /// is only ever an optimization (letting one process do some expensive work while everyone else waits and reuses the result, instead of every process
    /// redoing it independently), never a correctness requirement, so callers must always have a safe fallback for when it can't be acquired.
    /// </summary>
    public static FileStream? TryAcquireExclusiveLock(string lockFilePath, TimeSpan timeout)
    {
        string? directory = Path.GetDirectoryName(lockFilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DateTime deadlineUtc = CurrentTimeUtc + timeout;

        while (true)
        {
            try
            {
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (CurrentTimeUtc >= deadlineUtc)
                {
                    return null;
                }

                Thread.Sleep(AcquireLockRetryDelay);
            }
        }
    }
}
