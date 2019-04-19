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

using System;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersChangeRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggersChangeRequest"/> class.
        /// </summary>
        /// <param name="name">Name of the logger to update</param>
        /// <param name="level">Minimum level to log - pass null to reset</param>
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
        /// Gets name(space) of logger level to change
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets minimum level to log, null to reset back to original
        /// </summary>
        public string Level { get; }

        public override string ToString()
        {
            return "[" + Name + "," + Level ?? "RESET" + "]";
        }
    }
}
