// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationProvider : ConfigurationProvider
{
    public PostProcessorConfigurationSource Source { get; }

    protected PostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
    {
        ArgumentGuard.NotNull(source);

        Source = source;
    }

    protected virtual void PostProcessConfiguration()
    {
        foreach (IConfigurationPostProcessor processor in Source.PostProcessors)
        {
            processor.PostProcessConfiguration(this, Data);
        }
    }
}
