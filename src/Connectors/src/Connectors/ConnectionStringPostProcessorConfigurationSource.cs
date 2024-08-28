// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringPostProcessorConfigurationSource(bool detectConfigurationChanges) : PostProcessorConfigurationSource, IConfigurationSource
{
    private readonly bool _detectConfigurationChanges = detectConfigurationChanges;

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        CaptureConfigurationBuilder(builder);
        return new ConnectionStringPostProcessorConfigurationProvider(this, _detectConfigurationChanges);
    }
}
