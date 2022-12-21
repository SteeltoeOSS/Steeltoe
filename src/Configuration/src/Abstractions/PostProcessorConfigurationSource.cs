// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal abstract class PostProcessorConfigurationSource
{
    public IList<IConfigurationPostProcessor> RegisteredProcessors { get; }

    public IConfigurationRoot ParentConfiguration { get; set; }

    protected PostProcessorConfigurationSource()
    {
        RegisteredProcessors = new List<IConfigurationPostProcessor>();
    }

    public void RegisterPostProcessor(IConfigurationPostProcessor processor)
    {
        if (processor == null)
        {
            throw new ArgumentNullException(nameof(processor));
        }

        RegisteredProcessors.Add(processor);
    }
}
