// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.Placeholder;

/// <summary>
/// Specialized implementation of ConfigurationRoot that does not call load on providers
/// </summary>
internal sealed class ConfigurationView : IConfigurationRoot
{
    private readonly IList<IConfigurationProvider> _providers;
    private readonly ConfigurationReloadToken _changeToken = new ();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationView"/> class.
    /// Initializes a Configuration root with a list of providers.
    /// </summary>
    /// <param name="providers">The <see cref="IConfigurationProvider"/>s for this configuration.</param>
    public ConfigurationView(IList<IConfigurationProvider> providers)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    /// <summary>
    /// Gets the <see cref="IConfigurationProvider"/>s for this configuration.
    /// </summary>
    public IEnumerable<IConfigurationProvider> Providers => _providers;

    /// <summary>
    /// Gets or sets the value corresponding to a configuration key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value.</returns>
    public string this[string key]
    {
        get => GetConfiguration(_providers, key);
        set => SetConfiguration(_providers, key, value);
    }

    /// <summary>
    /// Gets the immediate children sub-sections.
    /// </summary>
    /// <returns>The children.</returns>
    public IEnumerable<IConfigurationSection> GetChildren() => _providers
        .Aggregate(
            Enumerable.Empty<string>(),
            (seed, source) => source.GetChildKeys(seed, null))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(GetSection);

    /// <summary>
    /// Returns a <see cref="IChangeToken"/> that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public IChangeToken GetReloadToken() => _changeToken;

    /// <summary>
    /// Gets a configuration sub-section with the specified key.
    /// </summary>
    /// <param name="key">The key of the configuration section.</param>
    /// <returns>The <see cref="IConfigurationSection"/>.</returns>
    /// <remarks>
    ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
    ///     an empty <see cref="IConfigurationSection"/> will be returned.
    /// </remarks>
    public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

    public void Reload()
    {
        // no-op - this provider is readonly
    }

    private static string GetConfiguration(IList<IConfigurationProvider> providers, string key)
    {
        for (var i = providers.Count - 1; i >= 0; i--)
        {
            var provider = providers[i];

            if (provider.TryGet(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static void SetConfiguration(IList<IConfigurationProvider> providers, string key, string value)
    {
        foreach (var provider in providers)
        {
            provider.Set(key, value);
        }
    }
}
