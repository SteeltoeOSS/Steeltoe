// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

public sealed class LogFileEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the path to the log file on disk. The path can be absolute, or relative to the directory that contains the application. When not set,
    /// this endpoint is turned off, unless it is explicitly enabled.
    /// </summary>
    public string? FilePath { get; set; }
}
