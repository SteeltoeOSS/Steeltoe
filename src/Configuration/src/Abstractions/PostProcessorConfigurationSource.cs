// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationSource
{
    private const string ParentConfigurationBuilderKey = "Steeltoe:PostProcessor:IsParentConfiguration";

    private static readonly LockPrimitive OwningSourcesLock = new();

    private readonly List<IConfigurationPostProcessor> _postProcessors = [];
    private IConfigurationBuilder? _capturedConfigurationBuilder;

    public IReadOnlyList<IConfigurationPostProcessor> PostProcessors => _postProcessors.AsReadOnly();
    public Predicate<string> IgnoreKeyPredicate { get; set; } = _ => false;

    public void RegisterPostProcessor(IConfigurationPostProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        _postProcessors.Add(processor);
    }

    protected void CaptureConfigurationBuilder(IConfigurationBuilder configurationBuilder)
    {
        // Don't retain the temporary builder used to build the parent configuration.
        if (!IsBuildingParentConfiguration(configurationBuilder))
        {
            _capturedConfigurationBuilder = configurationBuilder;
        }
    }

    /// <summary>
    /// Determines whether the specified builder is used to build a temporary copy of the parent configuration, in which case the providers created from it
    /// must not dispose the post-processors, which are shared with the providers of the actual configuration.
    /// </summary>
    protected static bool IsBuildingParentConfiguration(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        return configurationBuilder.Properties.ContainsKey(ParentConfigurationBuilderKey);
    }

    /// <summary>
    /// Determines whether the specified builder is used to build a temporary copy of the parent configuration for this type of source. Such a copy must not
    /// contain this source, which could otherwise (when wrapped in a <see cref="ICompositeConfigurationSource" />) build its own parent configuration again,
    /// endlessly.
    /// </summary>
    protected bool IsBuildingOwnParentConfiguration(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        return configurationBuilder.Properties.TryGetValue(ParentConfigurationBuilderKey, out object? owner) && ReferenceEquals(owner, GetType());
    }

    /// <summary>
    /// Builds a temporary copy of the configuration that was captured when this source was built, excluding sources of the same type as this source. The
    /// caller is responsible for disposing the returned configuration.
    /// </summary>
    public ConfigurationRoot GetParentConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (_capturedConfigurationBuilder != null)
        {
            CopySources(_capturedConfigurationBuilder, configurationBuilder);

            foreach (KeyValuePair<string, object> pair in _capturedConfigurationBuilder.Properties)
            {
                configurationBuilder.Properties.Add(pair);
            }
        }

        configurationBuilder.Properties[ParentConfigurationBuilderKey] = GetType();

        return BuildWithoutOwningChainedConfigurations(configurationBuilder);
    }

    private void CopySources(IConfigurationBuilder source, ConfigurationBuilder destination)
    {
        foreach (IConfigurationSource configurationSource in source.Sources)
        {
            if (configurationSource.GetType() != GetType())
            {
                if (configurationSource is ChainedConfigurationSource { ShouldDisposeConfiguration: true, Configuration: { } chainedConfiguration })
                {
                    // The temporary copy must not take over ownership of a chained configuration (such as the host configuration of HostBuilder),
                    // because disposing the copy would then dispose providers that the actual configuration still uses.

                    destination.Add(new ChainedConfigurationSource
                    {
                        Configuration = chainedConfiguration,
                        ShouldDisposeConfiguration = false
                    });
                }
                else
                {
                    destination.Add(configurationSource);
                }
            }
        }
    }

    private static ConfigurationRoot BuildWithoutOwningChainedConfigurations(ConfigurationBuilder configurationBuilder)
    {
        // Composite sources (such as the ones added by AddPlaceholderResolver and AddDecryption) wrap the chained source and rebuild it from the same
        // instance, which can't be replaced. Instead, temporarily clear the ownership flag on those instances while building the copy. The flag is read
        // when the chained provider is created, so it can be restored right after building.
        List<ChainedConfigurationSource> owningSources =
        [
            .. configurationBuilder.EnumerateSources<ChainedConfigurationSource>().Where(source => source.ShouldDisposeConfiguration)
        ];

        if (owningSources.Count == 0)
        {
            return (ConfigurationRoot)configurationBuilder.Build();
        }

        lock (OwningSourcesLock)
        {
            foreach (ChainedConfigurationSource source in owningSources)
            {
                source.ShouldDisposeConfiguration = false;
            }

            try
            {
                return (ConfigurationRoot)configurationBuilder.Build();
            }
            finally
            {
                foreach (ChainedConfigurationSource source in owningSources)
                {
                    source.ShouldDisposeConfiguration = true;
                }
            }
        }
    }
}
