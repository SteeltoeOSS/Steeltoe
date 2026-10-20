// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Text;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Configuration;

/// <summary>
/// Creates temporary files with secret content (such as certificates and private keys) and deletes them on dispose. A file is created once per distinct
/// content, so that repeatedly processing the same configuration (which happens each time the configuration reloads) does not create new files.
/// </summary>
internal sealed class TempFileRegistry : IDisposable
{
    private readonly LockPrimitive _lock = new();
    private readonly Dictionary<string, string> _pathsByContentHash = [];

    /// <summary>
    /// Gets the path of a temporary file that contains the specified content, creating it when it does not exist yet.
    /// </summary>
    public string GetOrCreate(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // The key is a hash, so that secrets are not retained as dictionary keys.
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

        lock (_lock)
        {
            if (_pathsByContentHash.TryGetValue(key, out string? path) && File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            using (StreamWriter writer = CreateTempFile(path))
            {
                writer.Write(content);
            }

            _pathsByContentHash[key] = path;
            return path;
        }
    }

    private static StreamWriter CreateTempFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            // Windows doesn't support setting umask.
            return File.CreateText(path);
        }

        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.Read,
            UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
        };

        var fileStream = new FileStream(path, options);
        return new StreamWriter(fileStream);
    }

    /// <summary>
    /// Deletes all temporary files. The registry can be used again afterward, which creates new files.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (string path in _pathsByContentHash.Values)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
                {
                    // Intentionally left empty.
                }
            }

            _pathsByContentHash.Clear();
        }
    }
}
