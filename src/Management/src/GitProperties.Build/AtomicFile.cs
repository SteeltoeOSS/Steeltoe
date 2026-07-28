// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Steeltoe.Management.GitProperties.Build;

/// <summary>
/// Safely reads, writes, and locks files that multiple projects and target frameworks in a solution build may touch at the same time.
/// </summary>
/// <remarks>
/// MSBuild builds multiple projects and target frameworks concurrently by default, so a reader can open a shared file mid-write, and two writers can
/// race to update it. Writes go through a swap that never leaves the file half-written, reads/writes retry briefly if they land on the wrong side of
/// someone else's swap, and an optional lock lets concurrent writers avoid redoing the same expensive work.
/// </remarks>
internal static class AtomicFile
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan ReadWriteRetryDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan AcquireLockRetryDelay = TimeSpan.FromMilliseconds(50);

#pragma warning disable S6354 // Use a testable date/time provider
    // Justification: System.TimeProvider isn't available on netstandard2.0.
    private static DateTime CurrentTimeUtc => DateTime.UtcNow;
#pragma warning restore S6354 // Use a testable date/time provider

    /// <summary>
    /// Writes the given lines to <paramref name="path" />. Even a concurrent reader only ever sees the complete previous content or the complete new
    /// content, never a partial write. Retries briefly if another process is momentarily in the way.
    /// </summary>
    public static void Write(string path, List<string> lines)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = Path.Combine(directory ?? string.Empty, $"{Path.GetRandomFileName()}~");
        var encoding = new UTF8Encoding(false);

        ExecuteWithRetry(() =>
        {
            File.WriteAllText(tempPath, $"{string.Join("\n", lines)}\n", encoding);
            MoveOrReplace(tempPath, path);
        }, ReadWriteRetryDelay, "write", path);
    }

    /// <summary>
    /// Reads all lines from <paramref name="path" />, retrying briefly if another process is momentarily in the way.
    /// </summary>
    public static string[] Read(string path)
    {
        return ExecuteWithRetry(() => File.ReadAllLines(path), ReadWriteRetryDelay, "read", path);
    }

    private static void MoveOrReplace(string sourcePath, string destinationPath)
    {
        try
        {
            File.Move(sourcePath, destinationPath);
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            File.Replace(sourcePath, destinationPath, null);
        }
    }

    /// <summary>
    /// Attempts to become the sole holder of <paramref name="lockFilePath" /> for up to <paramref name="timeout" />. Returns <c>null</c> if that doesn't
    /// happen in time, or if locking isn't possible at all. Holding this lock is only ever an optimization, never a correctness requirement, so callers must
    /// always have a safe fallback for when it can't be acquired.
    /// </summary>
    public static FileStream? TryAcquireExclusiveLock(string lockFilePath, TimeSpan timeout)
    {
        string? directory = Path.GetDirectoryName(lockFilePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DateTime deadlineUtc = CurrentTimeUtc + timeout;

        return ExecuteWithTimeoutRetry(() => new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), deadlineUtc,
            AcquireLockRetryDelay);
    }

    // These retry harnesses are only reachable when a real transient I/O error occurs mid-build, which tests can't reliably induce.
    [ExcludeFromCodeCoverage]
    private static void ExecuteWithRetry(Action action, TimeSpan retryDelay, string operation, string path)
    {
        ExecuteWithRetry<object?>(() =>
        {
            action();
            return null;
        }, retryDelay, operation, path);
    }

    [ExcludeFromCodeCoverage]
    private static T ExecuteWithRetry<T>(Func<T> action, TimeSpan retryDelay, string operation, string path)
    {
        Exception? lastError = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsTransientError(exception))
            {
                lastError = exception;
                Thread.Sleep(retryDelay);
            }
        }

        throw new IOException($"Failed to {operation} {path} after {MaxAttempts} attempts.", lastError);
    }

    [ExcludeFromCodeCoverage]
    private static FileStream? ExecuteWithTimeoutRetry(Func<FileStream> action, DateTime deadlineUtc, TimeSpan retryDelay)
    {
        while (true)
        {
            try
            {
                return action();
            }
            catch (Exception exception) when (IsTransientError(exception))
            {
                if (CurrentTimeUtc >= deadlineUtc)
                {
                    return null;
                }

                Thread.Sleep(retryDelay);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private static bool IsTransientError(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException;
    }
}
