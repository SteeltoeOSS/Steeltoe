// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Connectors;

public sealed class ConnectorSetupOptions
{
    public bool EnableHealthChecks { get; set; } = true;
    public ConnectorCreateConnection? CreateConnection { get; set; }
    public ConnectorCreateHealthContributor? CreateHealthContributor { get; set; }
}
