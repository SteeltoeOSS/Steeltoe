// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

using Filter = System.Func<string, Microsoft.Extensions.Logging.LogLevel, bool>;

namespace Steeltoe.Extensions.Logging
{
    public class InitialLevels
    {
        public IDictionary<string, LogLevel> OriginalLevels { get; set; }

        public IDictionary<string, Filter> RunningLevelFilters { get; set; }

        public Filter DefaultLevelFilter { get; set; }
    }
}
