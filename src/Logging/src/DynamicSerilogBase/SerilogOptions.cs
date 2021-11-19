// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog.Events;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Extensions.Logging.DynamicSerilog
{
    /// <summary>
    /// Implements a subset of the Serilog Options needed for SerilogDynamicProvider
    /// </summary>
    public class SerilogOptions : ISerilogOptions
    {
        public string ConfigPath => "Serilog";

        /// <summary>
        /// Gets or sets the minimum level for the root logger (and the "Default").
        /// Limits the verbosity of all other overrides to this setting
        /// </summary>
        public MinimumLevel MinimumLevel { get; set; }

        public IEnumerable<string> SubloggerConfigKeyExclusions { get; set; }

        public SerilogOptions(IConfiguration configuration)
        {
            var section = configuration.GetSection(ConfigPath);
            section.Bind(this);
            if (MinimumLevel == null || MinimumLevel.Default == (LogEventLevel)(-1))
            {
                MinimumLevel = new MinimumLevel()
                {
                    Default = LogEventLevel.Verbose, // Set root to verbose to have sub loggers work at all levels
                    Override = new Dictionary<string, LogEventLevel>()
                };
            }

            if (SubloggerConfigKeyExclusions == null)
            {
                SubloggerConfigKeyExclusions = new List<string> { "WriteTo", "MinimumLevel" };
            }
        }

        public IEnumerable<string> FullnameExclusions => SubloggerConfigKeyExclusions?.Select(key => ConfigPath + ":" + key);
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class MinimumLevel
#pragma warning restore SA1402 // File may only contain a single class
    {
        public LogEventLevel Default { get; set; } = (LogEventLevel)(-1);

        public Dictionary<string, LogEventLevel> Override { get; set; }
    }
}
