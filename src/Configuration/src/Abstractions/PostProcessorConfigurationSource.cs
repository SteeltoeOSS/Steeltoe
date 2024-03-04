// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationSource
{
    private readonly List<IConfigurationPostProcessor> _postProcessors = [];
    private IConfigurationBuilder? _capturedConfigurationBuilder;

    public IReadOnlyList<IConfigurationPostProcessor> PostProcessors => _postProcessors.AsReadOnly();
    public Predicate<string> IgnoreKeyPredicate { get; set; } = _ => false;

    public void RegisterPostProcessor(IConfigurationPostProcessor processor)
    {
        ArgumentGuard.NotNull(processor);

        _postProcessors.Add(processor);
    }

    protected void CaptureConfigurationBuilder(IConfigurationBuilder configurationBuilder)
    {
        _capturedConfigurationBuilder = configurationBuilder;
    }

    public IConfigurationRoot GetParentConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();

        if (_capturedConfigurationBuilder != null)
        {
            foreach (IConfigurationSource source in _capturedConfigurationBuilder.Sources)
            {
                if (source.GetType() != GetType())
                {
                    configurationBuilder.Add(source);
                }
            }

            foreach (KeyValuePair<string, object> pair in _capturedConfigurationBuilder.Properties)
            {
                configurationBuilder.Properties.Add(pair);
            }
        }

        return configurationBuilder.Build();
    }
}
