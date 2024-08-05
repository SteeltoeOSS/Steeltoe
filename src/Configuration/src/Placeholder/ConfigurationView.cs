// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Configuration.Placeholder;

/// <summary>
/// Specialized implementation of <see cref="IConfigurationRoot" /> that does not call <see cref="Reload" /> on providers.
/// </summary>
internal sealed class ConfigurationView : IConfigurationRoot
{
    private readonly IList<IConfigurationProvider> _providers;
    private readonly ConfigurationReloadToken _changeToken = new();

    /// <summary>
    /// Gets the <see cref="IConfigurationProvider" />s for this configuration.
    /// </summary>
    public IEnumerable<IConfigurationProvider> Providers => _providers;

    /// <summary>
    /// Gets or sets the value corresponding to a configuration key.
    /// </summary>
    /// <param name="key">
    /// The configuration key.
    /// </param>
    /// <returns>
    /// The configuration value.
    /// </returns>
    public string? this[string key]
    {
        get => GetConfiguration(_providers, key);
        set => SetConfiguration(_providers, key, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationView" /> class from a list of providers.
    /// </summary>
    /// <param name="providers">
    /// The <see cref="IConfigurationProvider" />s for this configuration.
    /// </param>
    public ConfigurationView(IList<IConfigurationProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = providers;
    }

    /// <summary>
    /// Gets the immediate child subsections.
    /// </summary>
    /// <returns>
    /// The children.
    /// </returns>
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return _providers.Aggregate(Enumerable.Empty<string>(), (seed, source) => source.GetChildKeys(seed, null)).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(GetSection);
    }

    /// <summary>
    /// Returns a <see cref="IChangeToken" /> that can be used to observe when this configuration is reloaded.
    /// </summary>
    /// <returns>
    /// The <see cref="IChangeToken" />.
    /// </returns>
    public IChangeToken GetReloadToken()
    {
        return _changeToken;
    }

    /// <summary>
    /// Gets a configuration subsection with the specified key.
    /// </summary>
    /// <remarks>
    /// This method will never return <c>null</c>. If no matching subsection is found with the specified key, an empty <see cref="IConfigurationSection" />
    /// will be returned.
    /// </remarks>
    /// <param name="key">
    /// The key of the configuration section.
    /// </param>
    /// <returns>
    /// The <see cref="IConfigurationSection" />.
    /// </returns>
    public IConfigurationSection GetSection(string key)
    {
        return new ConfigurationSection(this, key);
    }

    public void Reload()
    {
        // Intentionally left empty - this provider is readonly.
    }

    private static string? GetConfiguration(IList<IConfigurationProvider> providers, string key)
    {
        for (int index = providers.Count - 1; index >= 0; index--)
        {
            IConfigurationProvider provider = providers[index];

            if (provider.TryGet(key, out string? value))
            {
                return value;
            }
        }

        return null;
    }

    private static void SetConfiguration(IList<IConfigurationProvider> providers, string key, string? value)
    {
        foreach (IConfigurationProvider provider in providers)
        {
            provider.Set(key, value);
        }
    }
}
