// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors;

public sealed class ConnectorConfigureOptionsBuilder
{
    /// <summary>
    /// Gets or sets a value indicating whether connection string changes are detected while the application is running. This is <c>false</c> by default to
    /// optimize startup performance. When set to <c>true</c>, existing configuration providers may get reloaded multiple times, potentially resulting in
    /// duplicate expensive calls. Be aware that detecting configuration changes only makes sense when
    /// <see cref="ConnectorAddOptionsBuilder.CacheConnection" /> is <c>false</c>.
    /// </summary>
    public bool DetectConfigurationChanges { get; set; }
}
