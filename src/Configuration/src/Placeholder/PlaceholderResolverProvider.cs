// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Configuration.Placeholder;

/// <summary>
/// Configuration provider that resolves placeholders. A placeholder takes the form of:
/// <code><![CDATA[
/// ${some:config:reference?default_if_not_present}
/// ]]></code>
/// </summary>
internal sealed class PlaceholderResolverProvider : IPlaceholderResolverProvider, IDisposable
{
    private readonly PropertyPlaceholderHelper _propertyPlaceholderHelper;
    private bool _isDisposed;

    public IList<IConfigurationProvider> Providers { get; } = new List<IConfigurationProvider>();
    public IList<string> ResolvedKeys { get; } = new List<string>();

    /// <summary>
    /// Gets the configuration this placeholder resolver wraps.
    /// </summary>
    public IConfigurationRoot? Configuration { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderResolverProvider" /> class. The new placeholder resolver wraps the provided configuration
    /// root.
    /// </summary>
    /// <param name="root">
    /// The configuration the provider uses when resolving placeholders.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public PlaceholderResolverProvider(IConfigurationRoot root, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Configuration = root;

        ILogger<PropertyPlaceholderHelper> placeholderHelperLogger = loggerFactory.CreateLogger<PropertyPlaceholderHelper>();
        _propertyPlaceholderHelper = new PropertyPlaceholderHelper(placeholderHelperLogger);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderResolverProvider" /> class. The new placeholder resolver wraps the provided configuration
    /// providers. The <see cref="Configuration" /> will be created from these providers.
    /// </summary>
    /// <param name="providers">
    /// The configuration providers the resolver uses when resolving placeholders.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    public PlaceholderResolverProvider(IList<IConfigurationProvider> providers, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Providers = providers;

        ILogger<PropertyPlaceholderHelper> placeholderHelperLogger = loggerFactory.CreateLogger<PropertyPlaceholderHelper>();
        _propertyPlaceholderHelper = new PropertyPlaceholderHelper(placeholderHelperLogger);
    }

    /// <summary>
    /// Tries to get a configuration value for the specified key. If the value is a placeholder, it will try to resolve the placeholder before returning it.
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
        ArgumentException.ThrowIfNullOrEmpty(key);
        EnsureInitialized();

        string? originalValue = Configuration![key];
        value = _propertyPlaceholderHelper.ResolvePlaceholders(originalValue, Configuration);

        if (value != originalValue && !ResolvedKeys.Contains(key))
        {
            ResolvedKeys.Add(key);
        }

        return value != null;
    }

    /// <summary>
    /// Sets a configuration value for the specified key. No placeholder resolution is performed.
    /// </summary>
    /// <param name="key">
    /// The configuration key whose value to set.
    /// </param>
    /// <param name="value">
    /// The configuration value to set at the specified key.
    /// </param>
    public void Set(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
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
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        Configuration ??= new ConfigurationRoot(Providers);
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            HashSet<IDisposable> disposables = [];

            foreach (IConfigurationProvider provider in Providers)
            {
                if (provider is IDisposable disposable)
                {
                    disposables.Add(disposable);
                }
            }

            if (Configuration != null)
            {
                foreach (IConfigurationProvider provider in Configuration.Providers)
                {
                    if (provider is IDisposable disposable)
                    {
                        disposables.Add(disposable);
                    }
                }
            }

            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }

            _isDisposed = true;
        }
    }
}
