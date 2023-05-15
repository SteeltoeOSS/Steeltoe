// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Configuration;

namespace Steeltoe.Connector;

internal sealed class ConnectionStringConfigurationSource2 : PostProcessorConfigurationSource, IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        ParentConfiguration ??= GetParentConfiguration(builder);
        return new ConnectionStringConfigurationProvider2(this);
    }
}

internal sealed class ConnectionStringConfigurationProvider2 : PostProcessorConfigurationProvider
{
    public ConnectionStringConfigurationProvider2(PostProcessorConfigurationSource source)
        : base(source)
    {
        ArgumentGuard.NotNull(source);
        ChangeToken.OnChange(() => source.ParentConfiguration.GetReloadToken(), _ => Load(), 0);
    }

    public override void Load()
    {
        PostProcessConfiguration();
    }
}
