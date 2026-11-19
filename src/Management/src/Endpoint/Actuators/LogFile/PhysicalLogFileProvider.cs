// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

/// <summary>
/// Provides access to log files on disk, using absolute paths. Unlike the default physical file provider, files are opened with sharing flags that allow
/// the logger to continue writing, roll over, or delete the file while it is being read.
/// </summary>
internal sealed class PhysicalLogFileProvider : IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        ArgumentException.ThrowIfNullOrEmpty(subpath);

        var fileInfo = new FileInfo(subpath);
        return fileInfo.Exists ? new PhysicalLogFileInfo(fileInfo) : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        ArgumentException.ThrowIfNullOrEmpty(subpath);

        var directoryInfo = new DirectoryInfo(subpath);
        return directoryInfo.Exists ? new PhysicalLogDirectoryContents(directoryInfo) : NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }

    private sealed class PhysicalLogDirectoryContents(DirectoryInfo directoryInfo) : IDirectoryContents
    {
        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return directoryInfo.EnumerateFiles().Select(IFileInfo (fileInfo) => new PhysicalLogFileInfo(fileInfo)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class PhysicalLogFileInfo(FileInfo fileInfo) : IFileInfo
    {
        public bool Exists => true;
        public bool IsDirectory => false;
        public string Name => fileInfo.Name;
        public string PhysicalPath => fileInfo.FullName;
        public long Length => fileInfo.Length;
        public DateTimeOffset LastModified => fileInfo.LastWriteTimeUtc;

        public Stream CreateReadStream()
        {
            // Share access with the logger, so it can continue to write, roll over, or delete the file while it is being read.
            return new FileStream(fileInfo.FullName, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = 0
            });
        }
    }
}
