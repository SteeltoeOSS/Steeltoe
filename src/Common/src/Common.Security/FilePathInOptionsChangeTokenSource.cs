// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Common.Security;

internal sealed class FilePathInOptionsChangeTokenSource<T> : IOptionsChangeTokenSource<T>
{
    private readonly IFileProvider _fileProvider;
    private string _filePath;
    private ConfigurationReloadToken _changeFilePathToken = new();

    public string Name { get; }

    public FilePathInOptionsChangeTokenSource(string? optionName, string filePath, IFileProvider fileProvider)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(fileProvider);

        Name = optionName ?? Microsoft.Extensions.Options.Options.DefaultName;
        _filePath = filePath;
        _fileProvider = fileProvider;
    }

    public void ChangePath(string filePath)
    {
        if (filePath == _filePath)
        {
            return;
        }

        _filePath = filePath;

        // Wait until the file is fully written to disk.
        Thread.Sleep(500);

        ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeFilePathToken, new ConfigurationReloadToken());
        previousToken.OnReload();
    }

    public IChangeToken GetChangeToken()
    {
        IChangeToken watcherChangeToken = _fileProvider.Watch(_filePath);

        return new CompositeChangeToken([
            watcherChangeToken,
            _changeFilePathToken
        ]);
    }
}
