// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Connectors.Test;

internal sealed class MemoryFileProvider : IFileProvider
{
    private readonly MemoryFileInfo _fileInfo;
    private ConfigurationReloadToken _changeToken = new();

    public MemoryFileProvider(MemoryFileInfo fileInfo)
    {
        _fileInfo = fileInfo;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        return _fileInfo;
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return new MemoryDirectoryContents(_fileInfo);
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

    private sealed class MemoryDirectoryContents : IDirectoryContents
    {
        private readonly IFileInfo _fileInfo;

        public bool Exists => true;

        public MemoryDirectoryContents(IFileInfo fileInfo)
        {
            _fileInfo = fileInfo;
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            yield return _fileInfo;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
