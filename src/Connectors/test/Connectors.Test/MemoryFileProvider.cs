// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;

namespace Steeltoe.Connectors.Test;

internal sealed class MemoryFileProvider : IFileProvider
{
    private static readonly char[] DirectorySeparators =
    {
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar
    };

    private readonly MemoryFileSystemEntry _root = MemoryFileSystemEntry.CreateDirectory("file-system-root");
    private ConfigurationReloadToken _changeToken = new();

    public void IncludeDirectory(string path)
    {
        ArgumentGuard.NotNullOrEmpty(path);

        IEnumerable<string> pathSegments = PathToSegments(path);
        _ = GetOrCreateDirectories(pathSegments);
    }

    public void IncludeFile(string path, byte[] contents)
    {
        ArgumentGuard.NotNullOrEmpty(path);
        ArgumentGuard.NotNull(contents);

        string[] pathSegments = PathToSegments(path).ToArray();
        string[] parentDirectories = pathSegments[..^1];
        string fileName = pathSegments[^1];

        MemoryFileSystemEntry parentDirectory = GetOrCreateDirectories(parentDirectories);
        var file = MemoryFileSystemEntry.CreateFile(fileName, contents);
        parentDirectory.Children.Add(file.Name, file);
    }

    private MemoryFileSystemEntry GetOrCreateDirectories(IEnumerable<string> pathSegments)
    {
        MemoryFileSystemEntry currentDirectory = _root;

        foreach (string segment in pathSegments)
        {
            if (!currentDirectory.Children.TryGetValue(segment, out MemoryFileSystemEntry? child))
            {
                child = MemoryFileSystemEntry.CreateDirectory(segment);
                currentDirectory.Children.Add(segment, child);
            }

            if (!child.IsDirectory)
            {
                throw new InvalidOperationException($"Unable to create directory '{segment}' because a file with the same name already exists.");
            }

            currentDirectory = child;
        }

        return currentDirectory;
    }

    public void ReplaceFile(string path, byte[] contents)
    {
        ArgumentGuard.NotNullOrEmpty(path);
        ArgumentGuard.NotNull(contents);

        MemoryFileSystemEntry? entry = Find(path);

        if (entry == null || entry.IsDirectory)
        {
            throw new InvalidOperationException($"File '{path}' does not exist.");
        }

        entry.ReplaceContents(contents);
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        ArgumentGuard.NotNullOrEmpty(subpath);

        MemoryFileSystemEntry? entry = Find(subpath);

        if (entry == null || entry.IsDirectory)
        {
            return new NotFoundFileInfo(subpath);
        }

        return entry;
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        ArgumentGuard.NotNullOrEmpty(subpath);

        MemoryFileSystemEntry? entry = Find(subpath);

        if (entry == null || !entry.IsDirectory)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        return entry;
    }

    private MemoryFileSystemEntry? Find(string path)
    {
        IEnumerable<string> pathSegments = PathToSegments(path);
        MemoryFileSystemEntry current = _root;

        foreach (string segment in pathSegments)
        {
            if (!current.IsDirectory || !current.Children.TryGetValue(segment, out MemoryFileSystemEntry? child))
            {
                return null;
            }

            current = child;
        }

        return current;
    }

    private static IEnumerable<string> PathToSegments(string path)
    {
        return path.TrimEnd(DirectorySeparators).Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    public IChangeToken Watch(string filter)
    {
        return _changeToken;
    }

    public void NotifyChanged()
    {
        ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }

    private sealed class MemoryFileSystemEntry : IDirectoryContents, IFileInfo
    {
        private byte[]? _fileContents;

        public bool Exists => true;
        public bool IsDirectory => _fileContents == null;
        public string Name { get; }
        public string PhysicalPath => null!;
        public long Length => _fileContents?.Length ?? -1;
        public DateTimeOffset LastModified => default;

        public IDictionary<string, MemoryFileSystemEntry> Children { get; } = new Dictionary<string, MemoryFileSystemEntry>(StringComparer.OrdinalIgnoreCase);

        private MemoryFileSystemEntry(string name, byte[]? fileContents)
        {
            Name = name;
            _fileContents = fileContents;
        }

        public static MemoryFileSystemEntry CreateFile(string name, byte[] contents)
        {
            return new MemoryFileSystemEntry(name, contents);
        }

        public static MemoryFileSystemEntry CreateDirectory(string name)
        {
            return new MemoryFileSystemEntry(name, null);
        }

        public Stream CreateReadStream()
        {
            if (IsDirectory)
            {
                throw new InvalidOperationException($"Unable to read file contents of '{Name}' because it is a directory.");
            }

            return new MemoryStream(_fileContents!);
        }

        public void ReplaceContents(byte[] contents)
        {
            ArgumentGuard.NotNull(contents);

            if (IsDirectory)
            {
                throw new InvalidOperationException($"Unable to replace file contents of '{Name}' because it is a directory.");
            }

            _fileContents = contents;
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return Children.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
