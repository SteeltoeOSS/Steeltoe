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
using System.Text.RegularExpressions;

namespace Steeltoe.Messaging.Rabbit.Config
{
    internal static class ConfigUtils
    {
        private static readonly Regex _regex = new Regex(@"\s+");

        public static bool IsExpression(string expression)
        {
            if (expression.StartsWith("#{") && expression.EndsWith("}"))
            {
                return true;
            }

            return false;
        }

        public static string ExtractExpressionString(string expression)
        {
            expression = expression.Substring(2, expression.Length - 2 - 1);
            return ReplaceWhitespace(expression, string.Empty);
        }

        public static bool IsServiceReference(string expression)
        {
            return expression[0] == '@';
        }

        public static string ExtractServiceName(string expression)
        {
            if (expression[0] == '@')
            {
                return expression.Substring(1);
            }

            return expression;
        }

        public static string ReplaceWhitespace(string input, string replacement)
        {
            return _regex.Replace(input, replacement);
        }
    }
}
