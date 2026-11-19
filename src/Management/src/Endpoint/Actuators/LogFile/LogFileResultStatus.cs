// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

public enum LogFileResultStatus
{
    /// <summary>
    /// The log file does not exist, or no log file is configured.
    /// </summary>
    NotFound,

    /// <summary>
    /// The entire log file is returned.
    /// </summary>
    Complete,

    /// <summary>
    /// The requested part of the log file is returned.
    /// </summary>
    Partial,

    /// <summary>
    /// The requested range does not overlap with the log file.
    /// </summary>
    RangeNotSatisfiable
}
