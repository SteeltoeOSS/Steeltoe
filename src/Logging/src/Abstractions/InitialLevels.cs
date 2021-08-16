// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging
{
    /// <summary>
    /// Initial set of LogLevels, Filters and DefaultFilter to initialize a <see cref="IDynamicLoggerProvider"/>
    /// </summary>
    public class InitialLevels
    {
        /// <summary>
        /// Gets or sets the original log levels by name space as specified in Configuration
        /// </summary>
        public IDictionary<string, LogLevel> OriginalLevels { get; set; }

        /// <summary>
        /// Gets or sets the initial runningLevel filters from configuration
        /// </summary>
        public IDictionary<string, Filter> RunningLevelFilters { get; set; }

        /// <summary>
        /// Gets or sets the DefaultLevelFilter for the root of all loggers
        /// </summary>
        public Filter DefaultLevelFilter { get; set; }
    }
}
