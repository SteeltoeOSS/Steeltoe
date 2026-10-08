// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Steeltoe.Configuration;
using LockPrimitive =
#if NET10_0_OR_GREATER
    System.Threading.Lock
#else
    object
#endif
    ;

namespace Steeltoe.Connectors;

internal sealed class ConnectionStringPostProcessorConfigurationProvider : PostProcessorConfigurationProvider
{
    private readonly IDisposable? _changeToken;
    private readonly LockPrimitive _parentConfigurationLock = new();
    private ConfigurationRoot? _lastParentConfiguration;
    private bool _isDisposed;

    public ConnectionStringPostProcessorConfigurationProvider(PostProcessorConfigurationSource source, bool detectConfigurationChanges)
        : base(source)
    {
        if (detectConfigurationChanges)
        {
            _changeToken = ChangeToken.OnChange(CreateParentConfigurationChangeToken, _ => Load(), 0);
        }
    }

    private IChangeToken CreateParentConfigurationChangeToken()
    {
        lock (_parentConfigurationLock)
        {
            if (_isDisposed)
            {
                // A change notification that was already in flight when this provider got disposed.
                return new CancellationChangeToken(CancellationToken.None);
            }

            ConfigurationRoot? previousParentConfiguration = _lastParentConfiguration;

            // Each time, a new copy of the parent configuration is created, which loads all its providers again. The previous copy, whose change token
            // has just fired, is no longer needed. Because the producer runs before the consumer, no copy is disposed while in use.
            _lastParentConfiguration = Source.GetParentConfiguration();
            previousParentConfiguration?.Dispose();

            return _lastParentConfiguration.GetReloadToken();
        }
    }

    public override void Load()
    {
        Data.Clear();
        PostProcessConfiguration();

        OnReload();
    }

    public override void Dispose()
    {
        _changeToken?.Dispose();

        lock (_parentConfigurationLock)
        {
            _isDisposed = true;
            _lastParentConfiguration?.Dispose();
            _lastParentConfiguration = null;
        }

        base.Dispose();
    }
}
