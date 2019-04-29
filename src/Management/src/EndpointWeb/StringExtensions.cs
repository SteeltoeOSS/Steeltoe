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

namespace Steeltoe.Management.Endpoint
{
    public static class StringExtensions
    {
        public static bool StartsWithSegments(this string incoming, string other, out string remaining)
        {
            string value1 = incoming ?? string.Empty;
            string value2 = other ?? string.Empty;
            if (value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase))
            {
                if (value1.Length == value2.Length || value1[value2.Length] == '/')
                {
                    remaining = value1.Substring(value2.Length);
                    return true;
                }
            }

            remaining = string.Empty;
            return false;
        }
    }
}
