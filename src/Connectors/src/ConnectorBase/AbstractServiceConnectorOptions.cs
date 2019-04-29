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
using System;
using System.Text;

namespace Steeltoe.CloudFoundry.Connector
{
    public abstract class AbstractServiceConnectorOptions
    {
        protected const char Default_Terminator = ';';
        protected const char Default_Separator = '=';
        private char _keyValueTerm;
        private char _keyValueSep;

        public AbstractServiceConnectorOptions(IConfiguration config, char terminator = Default_Terminator, char separator = Default_Separator)
            : this(terminator, separator)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.Bind(this);
        }

        protected AbstractServiceConnectorOptions()
            : this(Default_Terminator, Default_Separator)
        {
        }

        protected AbstractServiceConnectorOptions(char keyValueTerm, char keyValueSep)
        {
            this._keyValueSep = keyValueSep;
            this._keyValueTerm = keyValueTerm;
        }

        /// <summary>
        /// Gets or sets a value indicating whether your service broker UrlEncodes service credentials
        /// </summary>
        public bool UrlEncodedCredentials { get; set; } = false;

        protected internal void AddKeyValue(StringBuilder sb, string key, int? value)
        {
            AddKeyValue(sb, key, value?.ToString());
        }

        protected internal void AddKeyValue(StringBuilder sb, string key, bool? value)
        {
            AddKeyValue(sb, key, value?.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Add a Key/Value pair to a <see cref="StringBuilder"/> if the value isn't null or empty
        /// </summary>
        /// <param name="sb">Your stringbuilder</param>
        /// <param name="key">Identifier for the value to be added</param>
        /// <param name="value">Value to be added</param>
        protected internal void AddKeyValue(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                sb.Append(key);
                sb.Append(_keyValueSep);
                sb.Append(value);
                sb.Append(_keyValueTerm);
            }
        }

        /// <summary>
        /// Add colon delimited pairs like user:password or host:port to a <see cref="StringBuilder"/>
        /// </summary>
        /// <param name="sb">Your stringbuilder</param>
        /// <param name="part1">First item in the pair</param>
        /// <param name="part2">Second item in the pair</param>
        /// <param name="terminator">Character to denote the end of the pair</param>
        /// <remarks>Only adds colon if second item is NOT null or empty</remarks>
        protected internal void AddColonDelimitedPair(StringBuilder sb, string part1, string part2, char? terminator = null)
        {
            sb.Append(part1);
            if (!string.IsNullOrEmpty(part2))
            {
                sb.Append(":");
                sb.Append(part2);
            }

            if (part1 != null && part2 != null && terminator != null)
            {
                sb.Append(terminator);
            }
        }
    }
}
