// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

#pragma warning disable S3881 // "IDisposable" should be implemented correctly

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationProvider : ConfigurationProvider, IDisposable
{
    public PostProcessorConfigurationSource Source { get; }

    protected PostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Source = source;
    }

    protected virtual void PostProcessConfiguration()
    {
        foreach (IConfigurationPostProcessor processor in Source.PostProcessors)
        {
            processor.PostProcessConfiguration(this, Data);
        }
    }

    public virtual void Dispose()
    {
        foreach (IConfigurationPostProcessor processor in Source.PostProcessors)
        {
            if (processor is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
