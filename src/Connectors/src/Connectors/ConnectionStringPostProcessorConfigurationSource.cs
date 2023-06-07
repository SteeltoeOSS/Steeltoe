// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringPostProcessorConfigurationSource : PostProcessorConfigurationSource, IConfigurationSource
{
    private readonly bool _detectConfigurationChanges;

    public ConnectionStringPostProcessorConfigurationSource(bool detectConfigurationChanges)
    {
        _detectConfigurationChanges = detectConfigurationChanges;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        CaptureConfigurationBuilder(builder);
        return new ConnectionStringPostProcessorConfigurationProvider(this, _detectConfigurationChanges);
    }
}
