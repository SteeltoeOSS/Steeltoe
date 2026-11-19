// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.LogFile;

public sealed class LogFileEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the path to the log file on disk. When not set, this endpoint is turned off, unless it is explicitly enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An absolute path is recommended. A relative path is resolved from the base directory of the application (<see cref="AppContext.BaseDirectory" />),
    /// which is not the same as the current directory that Serilog and Spring Boot resolve relative paths from. When running from an IDE or using
    /// <c>dotnet run</c>, that is typically a build output folder such as <c>bin/Debug/net10.0</c>.
    /// </para>
    /// <para>
    /// The file name (but not the directory) can contain the wildcards <c>*</c> and <c>?</c>. The matching file with the most recent last modified time is
    /// returned. Use this when the logging framework doesn't keep a fixed name for the current log file. For example, Serilog's file sink with
    /// <c>fileSizeLimitBytes</c> and <c>rollOnFileSizeLimit</c> writes to <c>app.log</c> until the limit is reached, then continues in <c>app_001.log</c>,
    /// <c>app_002.log</c>, and so on. Using <c>/var/log/app*.log</c> returns the file that is currently being written to, whereas <c>/var/log/app.log</c> would
    /// stop receiving new entries after the first roll over. The same applies when <c>rollingInterval</c> adds a date to the file name.
    /// </para>
    /// </remarks>
    public string? FilePath { get; set; }
}
