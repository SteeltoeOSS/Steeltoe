// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Configuration;

internal abstract partial class CompositeConfigurationProvider : IConfigurationProvider, IDisposable
{
    private readonly IList<IConfigurationProvider> _providers;
    private readonly ILogger<CompositeConfigurationProvider> _logger;
    private bool _isDisposed;

    protected internal IConfigurationRoot? ConfigurationRoot { get; private set; }

    protected CompositeConfigurationProvider(IList<IConfigurationProvider> providers, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _providers = providers;
        _logger = loggerFactory.CreateLogger<CompositeConfigurationProvider>();
    }

    public IChangeToken GetReloadToken()
    {
        LogGetReloadToken(GetType().Name);
        return ConfigurationRoot?.GetReloadToken()!;
    }

    public void Load()
    {
        Load(ConfigurationRoot != null);
    }

    private void Load(bool isReload)
    {
        LogLoad(GetType().Name, isReload);

        if (isReload)
        {
            ConfigurationRoot!.Reload();
        }
        else
        {
            LogCreateConfigurationRoot(GetType().Name, _providers.Count);
            ConfigurationRoot = new ConfigurationRoot(_providers);
        }
    }

    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        string[] earlierKeysArray = earlierKeys as string[] ?? earlierKeys.ToArray();
#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
        ArgumentNullException.ThrowIfNull(earlierKeysArray, nameof(earlierKeys));
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly

        LogGetChildKeys(GetType().Name, earlierKeysArray, parentPath);

        IConfiguration? section = parentPath == null ? ConfigurationRoot : ConfigurationRoot?.GetSection(parentPath);

        if (section == null)
        {
            return earlierKeysArray;
        }

        List<string> keys = [];
        keys.AddRange(section.GetChildren().Select(child => child.Key));
        keys.AddRange(earlierKeysArray);
        keys.Sort(ConfigurationKeyComparer.Instance);
        return keys;
    }

    public virtual bool TryGet(string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        LogTryGet(GetType().Name, key);

        value = ConfigurationRoot?.GetValue<string>(key);
        bool found = value != null;

        if (found)
        {
            LogTryGetFound(GetType().Name, key, value!);
        }

        return found;
    }

    public void Set(string key, string? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        LogSet(GetType().Name, key, value);
        ConfigurationRoot?[key] = value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed && ConfigurationRoot is IDisposable disposable)
        {
            _isDisposed = true;

            LogDispose(GetType().Name);
            disposable.Dispose();
        }
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "GetReloadToken from {Type}.")]
    private partial void LogGetReloadToken(string type);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Load from {Type} with isReload {IsReload}.")]
    private partial void LogLoad(string type, bool isReload);

    [LoggerMessage(Level = LogLevel.Trace, Message = "CreateConfigurationRoot from {Type} with {ProviderCount} providers.")]
    private partial void LogCreateConfigurationRoot(string type, int providerCount);

    [LoggerMessage(Level = LogLevel.Trace, Message = "GetChildKeys from {Type} with earlierKeys [{EarlierKeys}] and parentPath '{ParentPath}'.")]
    private partial void LogGetChildKeys(string type, string[] earlierKeys, string? parentPath);

    [LoggerMessage(Level = LogLevel.Trace, Message = "TryGet from {Type} with key '{Key}'.")]
    private partial void LogTryGet(string type, string key);

    [LoggerMessage(Level = LogLevel.Trace, Message = "TryGet from {Type} with key '{Key}' found value '{Value}'.")]
    private partial void LogTryGetFound(string type, string key, string value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Set from {Type} with key '{Key}' and value '{Value}'.")]
    private partial void LogSet(string type, string key, string? value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Dispose from {Type}.")]
    private partial void LogDispose(string type);
}
