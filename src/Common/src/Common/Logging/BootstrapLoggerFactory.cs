// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Logging;

/// <summary>
/// Provides access to logging infrastructure before service container is created. Any loggers created are upgraded with config settings after
/// config subsystem and later DI subsystem are available.
/// This class should only be used by components that need logging logging infrastructure before service container is available.
/// </summary>
public static class BootstrapLoggerFactory
{
    public static IBootstrapLoggerFactory Instance { get; } = new UpgradableBootstrapLoggerFactory();
}
