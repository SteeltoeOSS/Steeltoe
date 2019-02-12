// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Extensions.Configuration
{
    /// <summary>
    /// Configuration source used in creating a <see cref="RandomValueProvider"/> that generates random numbers
    /// </summary>
    public class RandomValueSource : IConfigurationSource
    {
        public const string PREFIX = "random:";
        internal ILoggerFactory _loggerFactory;
        internal string _prefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomValueSource"/> class.
        /// </summary>
        /// <param name="logFactory">the logger factory to use</param>
        public RandomValueSource(ILoggerFactory logFactory = null)
            : this(PREFIX, logFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomValueSource"/> class.
        /// </summary>
        /// <param name="prefix">key prefix to use to match random number keys. Should end with the configuration seperator</param>
        /// <param name="logFactory">the logger factory to use</param>
        public RandomValueSource(string prefix, ILoggerFactory logFactory = null)
        {
            _loggerFactory = logFactory;
            _prefix = prefix;
        }

        /// <summary>
        /// Builds a <see cref="RandomValueProvider"/> from the sources.
        /// </summary>
        /// <param name="builder">the provided builder</param>
        /// <returns>the random number provider</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new RandomValueProvider(_prefix, _loggerFactory);
        }
    }
}
