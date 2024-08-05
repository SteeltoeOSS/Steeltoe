// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Common.Logging;

/// <summary>
/// Provides access to logging infrastructure before service container is created. Any loggers created are updated with configuration settings after
/// configuration subsystem and later DI subsystem are available. This class should only be used by components that need logging infrastructure before
/// the service container is available.
/// </summary>
public static class BootstrapLoggerFactory
{
    // Intended for integration tests, to prevent them from influencing each other.
    private static readonly ConcurrentDictionary<object, IBootstrapLoggerFactory> InstanceMap = new();

    public static IBootstrapLoggerFactory Default { get; } = new UpgradableBootstrapLoggerFactory();

    public static IBootstrapLoggerFactory GetInstance(object contextKey)
    {
        ArgumentNullException.ThrowIfNull(contextKey);

        return InstanceMap.GetOrAdd(contextKey, _ => new UpgradableBootstrapLoggerFactory());
    }
}
