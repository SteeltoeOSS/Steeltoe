// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;

namespace Steeltoe.Configuration.Encryption;

/// <summary>
/// Configuration provider that resolves encryptions. An encryption takes the form of:
/// <code><![CDATA[
/// ${some:config:reference?default_if_not_present}
/// ]]></code>
/// </summary>
internal sealed class EncryptionResolverProvider : IConfigurationProvider, IDisposable
{
    // regex for matching {cipher}{key:keyAlias} at the start of the string
    private readonly Regex _cipherRegex = new("^{cipher}({key:(?<alias>.*)})?(?<cipher>.*)");
    private bool _isDisposed;

    /// <summary>
    /// Gets the configuration this encryption resolver wraps.
    /// </summary>
    internal IConfigurationRoot? Configuration { get; private set; }

    public IList<IConfigurationProvider> Providers { get; } = new List<IConfigurationProvider>();

    public ITextDecryptor Decryptor { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionResolverProvider" /> class. The new encryption resolver wraps the provided configuration root.
    /// </summary>
    /// <param name="root">
    /// The configuration the provider uses when resolving encryptions.
    /// </param>
    /// <param name="textDecryptor">
    /// The decryptor to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public EncryptionResolverProvider(IConfigurationRoot root, ITextDecryptor textDecryptor, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(root);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        Configuration = root;
        Decryptor = textDecryptor;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionResolverProvider" /> class. The new encryption resolver wraps the provided configuration
    /// providers. The <see cref="Configuration" /> will be created from these providers.
    /// </summary>
    /// <param name="providers">
    /// The configuration providers the resolver uses when resolving encryptions.
    /// </param>
    /// <param name="textDecryptor">
    /// The decryptor to use.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public EncryptionResolverProvider(IList<IConfigurationProvider> providers, ITextDecryptor textDecryptor, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(providers);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(textDecryptor);

        Providers = providers;
        Decryptor = textDecryptor;
    }

    /// <summary>
    /// Tries to get a configuration value for the specified key. If the value is an encryption, it will try to resolve the encryption before returning it.
    /// </summary>
    /// <param name="key">
    /// The configuration key.
    /// </param>
    /// <param name="value">
    /// When this method returns, contains the resolved configuration value, if a value for the specified key was found.
    /// </param>
    /// <returns>
    /// <c>true</c> if a value for the specified key was found, otherwise <c>false</c>.
    /// </returns>
    public bool TryGet(string key, out string? value)
    {
        ArgumentGuard.NotNull(key);
        EnsureInitialized();

        string? originalValue = Configuration![key];
        value = originalValue;

        if (!string.IsNullOrEmpty(originalValue))
        {
            Match match = _cipherRegex.Match(originalValue);

            if (match.Success)
            {
                string alias = match.Groups["alias"].Value;
                string cipher = match.Groups["cipher"].Value;

                value = !string.IsNullOrEmpty(alias) ? Decryptor.Decrypt(cipher, alias) : Decryptor.Decrypt(cipher);
            }
        }

        return !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Sets a configuration value for the specified key. No encryption resolution is performed.
    /// </summary>
    /// <param name="key">
    /// The configuration key whose value to set.
    /// </param>
    /// <param name="value">
    /// The configuration value to set at the specified key.
    /// </param>
    public void Set(string key, string? value)
    {
        ArgumentGuard.NotNull(key);
        EnsureInitialized();

        Configuration![key] = value;
    }

    /// <summary>
    /// Returns a change token that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>
    /// The change token.
    /// </returns>
    public IChangeToken GetReloadToken()
    {
        EnsureInitialized();

        return Configuration!.GetReloadToken();
    }

    /// <summary>
    /// Creates the <see cref="Configuration" /> from the providers, if it has not done so already, and calls <see cref="IConfigurationRoot.Reload" /> on the
    /// underlying configuration.
    /// </summary>
    public void Load()
    {
        EnsureInitialized();

        Configuration!.Reload();
    }

    /// <summary>
    /// Returns the immediate descendant configuration keys for a given parent path, based on this <see cref="Configuration" />'s data and the set of keys
    /// returned by all the preceding providers.
    /// </summary>
    /// <param name="earlierKeys">
    /// The child keys returned by the preceding providers for the same parent path.
    /// </param>
    /// <param name="parentPath">
    /// The parent path.
    /// </param>
    /// <returns>
    /// The child keys.
    /// </returns>
    public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        EnsureInitialized();

        IConfiguration section = parentPath == null ? Configuration! : Configuration!.GetSection(parentPath);
        IEnumerable<IConfigurationSection> children = section.GetChildren();

        return children.Select(childSection => childSection.Key).Concat(earlierKeys).OrderBy(key => key, ConfigurationKeyComparer.Instance);
    }

    private void EnsureInitialized()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(EncryptionResolverProvider));
        }

        Configuration ??= new ConfigurationRoot(Providers);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            HashSet<IDisposable> disposables = [];

            foreach (IDisposable disposable in GetDisposables(Providers))
            {
                disposables.Add(disposable);
            }

            if (Configuration != null)
            {
                foreach (IDisposable disposable in GetDisposables(Configuration.Providers))
                {
                    disposables.Add(disposable);
                }
            }

            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }

            _isDisposed = true;
        }
    }

    private HashSet<IDisposable> GetDisposables(IEnumerable<IConfigurationProvider> providers)
    {
        HashSet<IDisposable> disposables = [];
        foreach (IConfigurationProvider provider in providers)
        {
            if (provider is IDisposable disposable)
            {
                disposables.Add(disposable);
            }
        }
        return disposables;
    }
}
