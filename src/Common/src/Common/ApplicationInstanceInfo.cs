// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

/// <inheritdoc />
public sealed class ApplicationInstanceInfo : IApplicationInstanceInfo
{
    /// <inheritdoc />
    public string? ApplicationName { get; set; }

    public string? ApplicationId { get; set; }
    public string? InstanceId { get; set; }
    public int InstanceIndex { get; set; } = -1;
    public IList<string> Uris { get; } = new List<string>();
    public string? InternalIP { get; set; }
}
