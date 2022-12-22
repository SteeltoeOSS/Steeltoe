// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationProvider : ConfigurationProvider
{
    public PostProcessorConfigurationSource Source { get; }

    protected PostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
    {
        Source = source;
    }

    protected virtual void PostProcessConfiguration()
    {
        foreach (IConfigurationPostProcessor processor in Source.RegisteredProcessors)
        {
            processor.PostProcessConfiguration(this, Data);
        }
    }
}
