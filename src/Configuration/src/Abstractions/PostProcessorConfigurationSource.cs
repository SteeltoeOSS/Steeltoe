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

    private static readonly object NestedConfigurationMarker = new();
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
    /// Marks the specified builder as one that builds a nested configuration, using sources that are shared with the actual configuration (such as the
    /// configuration that Config Server uses to determine how to connect). Providers created from it neither recapture the builder nor dispose the
    /// post-processors that are shared with the providers of the actual configuration, and connection strings aren't post-processed in it.
    /// </summary>
    public static void MarkAsNestedConfiguration(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties[ParentConfigurationBuilderKey] = NestedConfigurationMarker;
    }

    /// <summary>
    /// Determines whether this source must be skipped, because the specified builder is used to build a temporary copy of the parent configuration for this
    /// type of source, or a nested configuration (see <see cref="MarkAsNestedConfiguration" />).
    /// </summary>
    /// <remarks>
    /// If this source is not skipped in these cases, it rebuilds the parent configuration. That parent configuration contains the same source, so the same
    /// steps repeat forever. This can happen in two situations. In the first, this source is wrapped in an <see cref="ICompositeConfigurationSource" />. In
    /// the second, another source in the parent configuration (such as Config Server) builds a nested configuration from the same sources.
    /// </remarks>
    protected bool IsBuildingOwnParentConfiguration(IConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        return configurationBuilder.Properties.TryGetValue(ParentConfigurationBuilderKey, out object? owner) &&
            (ReferenceEquals(owner, GetType()) || ReferenceEquals(owner, NestedConfigurationMarker));
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

    /// <summary>
    /// Builds the configuration from the specified builder, without the resulting providers taking over ownership of chained configurations (such as the
    /// host configuration of HostBuilder). Use this when building a configuration from sources that are shared with the actual configuration, which remains
    /// responsible for disposing the chained configurations.
    /// </summary>
    public static ConfigurationRoot BuildWithoutOwningChainedConfigurations(ConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

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
