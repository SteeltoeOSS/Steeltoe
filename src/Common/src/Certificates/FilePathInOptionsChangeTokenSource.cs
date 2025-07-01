// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Common.Certificates;

internal sealed class FilePathInOptionsChangeTokenSource<T> : IOptionsChangeTokenSource<T>, IDisposable
{
    private readonly FileSystemChangeWatcher _fileWatcher = new();
    private string _filePath;
    private ConfigurationReloadToken _changeFilePathToken = new();

    public string Name { get; }

    public FilePathInOptionsChangeTokenSource(string? optionName, string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        Name = optionName ?? Options.DefaultName;
        _filePath = filePath;
    }

    public void ChangePath(string filePath)
    {
        if (filePath != _filePath)
        {
            _filePath = filePath;

            ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _changeFilePathToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }
    }

    public IChangeToken GetChangeToken()
    {
        IChangeToken watcherChangeToken = _fileWatcher.GetChangeToken(_filePath);

        // Wrap the watcher token to delay signaling to the options monitor
        // -- avoids IOException when certificate and key change around the same time.
        IChangeToken debouncedToken = new DebouncedChangeToken(watcherChangeToken, TimeSpan.FromMilliseconds(200));

        return new CompositeChangeToken([
            debouncedToken,
            _changeFilePathToken
        ]);
    }

    public void Dispose()
    {
        _fileWatcher.Dispose();
    }

    private sealed class FileSystemChangeWatcher : IDisposable
    {
        private PhysicalFilesWatcher? _filesWatcher;
        private string? _previousFilePath;

        public IChangeToken GetChangeToken(string filePath)
        {
            if (filePath == _previousFilePath)
            {
                if (_filesWatcher == null)
                {
                    // Determined earlier that this path is not watchable.
                    return NullChangeToken.Singleton;
                }

                // Return new token for the same file.
                string absolutePath = Path.GetFullPath(filePath);
                string fileName = Path.GetFileName(absolutePath);
                return _filesWatcher.CreateFileChangeToken(fileName);
            }

            // Path has changed. Stop watching the previous file, if any.
            _previousFilePath = filePath;
            Dispose();

            if (filePath.Length > 0)
            {
                string absolutePath = Path.GetFullPath(filePath);
                string? directory = Path.GetDirectoryName(absolutePath);

                if (directory != null)
                {
                    string root = EnsureTrailingSlash(directory);

                    // Because PhysicalFilesWatcher implicitly disposes FileSystemWatcher (despite not owning it),
                    // we can't just update the existing FileSystemWatcher.Path, but instead we must recreate everything.
                    var fileSystemWatcher = new FileSystemWatcher(root);

                    try
                    {
                        // This throws on macOS, because it requires polling. But to make polling actually work, we need to
                        // set the internal UseActivePolling property. See https://github.com/dotnet/runtime/issues/48602.
                        _filesWatcher = new PhysicalFilesWatcher(root, fileSystemWatcher, false);

                        string fileName = Path.GetFileName(absolutePath);
                        return _filesWatcher.CreateFileChangeToken(fileName);
                    }
                    catch (Exception)
                    {
                        Dispose();
                        fileSystemWatcher.Dispose();
                        throw;
                    }
                }
            }

            // New path is not watchable.
            return NullChangeToken.Singleton;
        }

        public void Dispose()
        {
            _filesWatcher?.Dispose();
            _filesWatcher = null;
        }

        private static string EnsureTrailingSlash(string path)
        {
            return path.Length > 0 && path[^1] != Path.DirectorySeparatorChar ? $"{path}{Path.DirectorySeparatorChar}" : path;
        }
    }

    private sealed class DebouncedChangeToken(IChangeToken inner, TimeSpan delay) : IChangeToken
    {
        private readonly IChangeToken _inner = inner;
        private readonly TimeSpan _delay = delay;

        public bool HasChanged => _inner.HasChanged;

        public bool ActiveChangeCallbacks => _inner.ActiveChangeCallbacks;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return _inner.RegisterChangeCallback(async void (_) =>
            {
                try
                {
                    await Task.Delay(_delay).ConfigureAwait(false);
                    callback(state);
                }
                catch
                {
                    // Swallow exceptions to avoid crashing the options infrastructure
                }
            }, state);
        }
    }
}
