// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationSource
{
    private const string ParentConfigurationBuilderKey = "Steeltoe:PostProcessor:IsParentConfiguration";

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
            foreach (IConfigurationSource source in _capturedConfigurationBuilder.Sources)
            {
                if (source.GetType() != GetType())
                {
                    if (source is ChainedConfigurationSource { ShouldDisposeConfiguration: true, Configuration: { } chainedConfiguration })
                    {
                        // The temporary copy must not take over ownership of a chained configuration (such as the host configuration of HostBuilder),
                        // because disposing the copy would then dispose providers that the actual configuration still uses.

                        configurationBuilder.Add(new ChainedConfigurationSource
                        {
                            Configuration = chainedConfiguration,
                            ShouldDisposeConfiguration = false
                        });
                    }
                    else
                    {
                        configurationBuilder.Add(source);
                    }
                }
            }

            foreach (KeyValuePair<string, object> pair in _capturedConfigurationBuilder.Properties)
            {
                configurationBuilder.Properties.Add(pair);
            }
        }

        configurationBuilder.Properties[ParentConfigurationBuilderKey] = GetType();

        return (ConfigurationRoot)configurationBuilder.Build();
    }
}
