// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Primitives;
using Steeltoe.Configuration;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringPostProcessorConfigurationProvider : PostProcessorConfigurationProvider, IDisposable
{
    private readonly IDisposable? _changeToken;

    public ConnectionStringPostProcessorConfigurationProvider(PostProcessorConfigurationSource source, bool detectConfigurationChanges)
        : base(source)
    {
        if (detectConfigurationChanges)
        {
            _changeToken = ChangeToken.OnChange(() => source.GetParentConfiguration().GetReloadToken(), _ => Load(), 0);
        }
    }

    public override void Load()
    {
        Data.Clear();
        PostProcessConfiguration();
        OnReload();
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
    }
}
