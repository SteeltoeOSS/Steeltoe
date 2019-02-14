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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Env
{
    public class Sanitizer
    {
        private readonly string[] regex_parts = new string[] { "*", "$", "^", "+" };
        private string[] _keysToSanitize;
        private List<Regex> _matchers = new List<Regex>();

        public Sanitizer(string [] keysToSanitize)
        {
            _keysToSanitize = keysToSanitize;

            foreach (var key in keysToSanitize)
            {
                var regexPattern = IsRegex(key) ? key : $".*{key}$";

                _matchers.Add(new Regex(regexPattern, RegexOptions.IgnoreCase));
            }
        }

        public KeyValuePair<string, string> Sanitize(KeyValuePair<string,string> kvp)
        {
            if (kvp.Value != null && _matchers.Any( m => m.IsMatch(kvp.Key)))
            {
                return new KeyValuePair<string, string>(kvp.Key, "******");
            }

            return kvp;
        }

        private bool IsRegex(string value)
        {
            foreach (var part in regex_parts)
            {
                if (value.Contains(part))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
