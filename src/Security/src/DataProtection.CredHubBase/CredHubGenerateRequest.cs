// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public abstract class CredHubGenerateRequest : CredHubBaseObject
{
    /// <summary>
    /// Gets or sets a value indicating the overwrite interaction mode
    /// </summary>
    public OverwiteMode Mode { get; set; } = OverwiteMode.converge;

    /// <summary>
    /// Gets or sets parameters for generating credential
    /// </summary>
    public object Parameters { get; set; }
}