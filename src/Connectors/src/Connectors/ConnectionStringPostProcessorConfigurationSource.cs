// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringPostProcessorConfigurationSource(bool detectConfigurationChanges) : PostProcessorConfigurationSource, IConfigurationSource
{
    private readonly bool _detectConfigurationChanges = detectConfigurationChanges;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // When this source is wrapped in a composite source (such as the placeholder resolver), the composite builds it again for the parent configuration.
        // Skip it there, because the parent configuration is used to post-process connection strings, which is what this source does. This presents infinite recursion.
        if (IsBuildingOwnParentConfiguration(builder))
        {
            return new MemoryConfigurationProvider(new MemoryConfigurationSource());
        }

        CaptureConfigurationBuilder(builder);

        return new ConnectionStringPostProcessorConfigurationProvider(this, _detectConfigurationChanges)
        {
            DisposesPostProcessors = !IsBuildingParentConfiguration(builder)
        };
    }
}
