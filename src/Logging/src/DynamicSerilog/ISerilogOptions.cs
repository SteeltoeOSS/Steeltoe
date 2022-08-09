// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Logging.DynamicSerilog;

public interface ISerilogOptions
{
    string ConfigPath { get; }

    MinimumLevel MinimumLevel { get; set; }

    [Obsolete("No longer needed with current implementation. Will be removed in next major release")]
    IEnumerable<string> SubLoggerConfigKeyExclusions { get; set; }

    [Obsolete("No longer needed with current implementation. Will be removed in next major release")]
    IEnumerable<string> FullNameExclusions { get; }
}