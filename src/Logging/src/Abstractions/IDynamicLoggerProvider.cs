// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging;

public interface IDynamicLoggerProvider : ILoggerProvider
{
    /// <summary>
    /// Get a list of all known namespaces and loggers.
    /// </summary>
    /// <returns>A collection of all known namespaces and loggers with their configurations.</returns>
    ICollection<ILoggerConfiguration> GetLoggerConfigurations();

    /// <summary>
    /// Set the logging threshold for a logger.
    /// </summary>
    /// <param name="category">A namespace or fully qualified logger name to adjust.</param>
    /// <param name="level">The minimum level that should be logged.</param>
    void SetLogLevel(string category, LogLevel? level);
}
