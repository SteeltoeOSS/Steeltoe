// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;

namespace Steeltoe.Connectors.Test;

internal sealed class MemoryFileInfo : IFileInfo
{
    private byte[] _contents;

    public bool Exists => true;
    public bool IsDirectory => false;
    public string Name { get; }
    public string PhysicalPath => null!;
    public long Length => _contents.Length;
    public DateTimeOffset LastModified => default;

    public MemoryFileInfo(string name, byte[] contents)
    {
        Name = name;
        _contents = contents;
    }

    public Stream CreateReadStream()
    {
        return new MemoryStream(_contents);
    }

    public void ReplaceContents(byte[] contents)
    {
        _contents = contents;
    }
}
