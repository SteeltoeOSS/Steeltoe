// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Loggers;

public sealed class LoggersRequest
{
    public LoggersRequestType Type { get; }

    /// <summary>
    /// Gets the name or namespace of the logger level to change.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the new minimum level to log at, or <c>null</c> to reset back to the original level.
    /// </summary>
    public string Level { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggersRequest" /> class for getting the log levels.
    /// </summary>
    public LoggersRequest()
    {
        Type = LoggersRequestType.Get;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggersRequest" /> class for changing a log level.
    /// </summary>
    /// <param name="name">
    /// The name or namespace of the logger level to change.
    /// </param>
    /// <param name="level">
    /// The minimum level to log, or <c>null</c> to reset back to the original level.
    /// </param>
    public LoggersRequest(string name, string level)
    {
        ArgumentGuard.NotNullOrEmpty(name);

        Type = LoggersRequestType.Change;
        Name = name;
        Level = level;
    }

    public override string ToString()
    {
        return Type == LoggersRequestType.Change ? $"Type={Type}, Name={Name}, Level={Level ?? "RESET"}" : $"Type={Type}";
    }
}
