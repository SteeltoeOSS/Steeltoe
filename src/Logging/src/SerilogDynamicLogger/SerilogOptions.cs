// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Serilog.Events;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger
{
    /// <summary>
    /// Implements a subset of the Serilog Options needed for SerilogDynamicProvider
    /// </summary>
    public class SerilogOptions : ISerilogOptions
    {
        private const string CONFIG_PATH = "Serilog";

        /// <summary>
        /// This controls the root logger (and the "Default") and
        /// limits the verbosity of all other overrides to this setting
        /// </summary>
        public MinimumLevel MinimumLevel { get; set; }

        public SerilogOptions(IConfiguration configuration)
        {
            var section = configuration.GetSection(CONFIG_PATH);
            section.Bind(this);
            if (MinimumLevel == null)
            {
                MinimumLevel = new MinimumLevel()
                {
                    Default = LogEventLevel.Verbose, // Set root to verbose to have sub loggers work at all levels
                    Override = new Dictionary<string, LogEventLevel>()
                };
            }
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public class MinimumLevel
#pragma warning restore SA1402 // File may only contain a single class
    {
        public LogEventLevel Default { get; set; }

        public Dictionary<string, LogEventLevel> Override { get; set; }
    }
}
