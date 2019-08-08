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

namespace Steeltoe.Extensions.Logging
{
    public class LoggerConfiguration : ILoggerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerConfiguration"/> class.
        /// </summary>
        /// <param name="name">Namespace</param>
        /// <param name="configured">Original log level</param>
        /// <param name="effective">Currently effective log level</param>
        public LoggerConfiguration(string name, LogLevel? configured, LogLevel effective)
        {
            Name = name;
            ConfiguredLevel = configured;
            EffectiveLevel = effective;
        }

        /// <summary>
        /// Gets namespace this configuration is applied to
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets level from base app configuration (if present)
        /// </summary>
        public LogLevel? ConfiguredLevel { get; }

        /// <summary>
        /// Gets running level of the logger
        /// </summary>
        public LogLevel EffectiveLevel { get; }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            LoggerConfiguration lc = obj as LoggerConfiguration;
            if (lc == null)
            {
                return false;
            }

            return this.Name == lc.Name &&
                this.ConfiguredLevel == lc.ConfiguredLevel &&
                this.EffectiveLevel == lc.EffectiveLevel;
        }

        public override string ToString()
        {
            return "[" + Name + "," + ConfiguredLevel + "," + EffectiveLevel + "]";
        }
    }
}