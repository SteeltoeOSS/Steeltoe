// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;
internal abstract class PostProcessorConfigurationProvider : ConfigurationProvider
{
    private readonly PostProcessorConfigurationSource _source;
    protected PostProcessorConfigurationProvider(PostProcessorConfigurationSource source)
    {
        _source = source;
    }

    protected virtual void PostProcessConfiguration()
    {
        foreach (var processor in _source.RegisteredProcessors)
        {
            processor.PostProcessConfiguration(this, Data);
        }
    }
}
