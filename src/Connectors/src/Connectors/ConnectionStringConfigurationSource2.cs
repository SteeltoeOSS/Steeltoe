// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringConfigurationSource2 : PostProcessorConfigurationSource, IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        ParentConfiguration ??= GetParentConfiguration(builder);
        return new ConnectionStringConfigurationProvider2(this);
    }
}
