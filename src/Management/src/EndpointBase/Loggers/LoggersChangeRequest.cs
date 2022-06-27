// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management.Endpoint.Loggers;

public class LoggersChangeRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggersChangeRequest"/> class.
    /// </summary>
    /// <param name="name">Name of the logger to update.</param>
    /// <param name="level">Minimum level to log - pass null to reset.</param>
    public LoggersChangeRequest(string name, string level)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(nameof(name));
        }

        Name = name;
        Level = level;
    }

    /// <summary>
    /// Gets name(space) of logger level to change.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets minimum level to log, null to reset back to original.
    /// </summary>
    public string Level { get; }

    public override string ToString()
    {
        return $"[{Name},{Level ?? "RESET"}]";
    }
}
