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

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Logging
{
    public interface IDynamicLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Get a list of all known namespaces and loggers
        /// </summary>
        /// <returns>A collection of all known namespaces and loggers with their configurations</returns>
        ICollection<ILoggerConfiguration> GetLoggerConfigurations();

        /// <summary>
        /// Set the logging threshold for a logger
        /// </summary>
        /// <param name="category">A namespace or fully qualified logger name to adjust</param>
        /// <param name="level">The minimum level that should be logged</param>
        void SetLogLevel(string category, LogLevel? level);
    }
}