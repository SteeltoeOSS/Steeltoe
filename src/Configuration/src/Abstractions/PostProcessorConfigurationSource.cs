// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationSource
{
    private readonly List<IConfigurationPostProcessor> _postProcessors = new();

    public IReadOnlyList<IConfigurationPostProcessor> PostProcessors => _postProcessors.AsReadOnly();
    public Predicate<string> IgnoreKeyPredicate { get; set; } = _ => false;
    public IConfigurationRoot ParentConfiguration { get; set; }

    public void RegisterPostProcessor(IConfigurationPostProcessor processor)
    {
        ArgumentGuard.NotNull(processor);

        _postProcessors.Add(processor);
    }

    protected IConfigurationRoot GetParentConfiguration(IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var configurationBuilder = new ConfigurationBuilder();

        foreach (IConfigurationSource source in builder.Sources)
        {
            if (source.GetType() != GetType())
            {
                configurationBuilder.Add(source);
            }
        }

        foreach (KeyValuePair<string, object> pair in builder.Properties)
        {
            configurationBuilder.Properties.Add(pair);
        }

        return configurationBuilder.Build();
    }
}
