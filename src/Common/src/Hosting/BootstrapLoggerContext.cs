// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Logging;

namespace Steeltoe.Common.Hosting;

/// <summary>
/// Provides a context to distinguish multiple <see cref="IBootstrapLoggerFactory" /> instances, which enables running from integration tests. Register
/// as a singleton, pointing to something that distinguishes for multiple usage (for example, your host builder).
/// </summary>
public sealed class BootstrapLoggerContext
{
    private readonly object _key;

    public BootstrapLoggerContext(object key)
    {
        ArgumentGuard.NotNull(key);

        _key = key;
    }

    public IBootstrapLoggerFactory GetBootstrapLoggerFactory()
    {
        return BootstrapLoggerFactory.GetInstance(_key);
    }
}
