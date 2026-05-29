// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Enables registering multiple typed providers to enumerate all <see cref="IOptionsMonitor{TOptions}" />s for the various
/// <see cref="EndpointOptions" /> types.
/// </summary>
internal interface IEndpointOptionsMonitorProvider
{
    EndpointOptions Get();
}
