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
using System.Text;

namespace Steeltoe.Common.Util
{
    public static class EncodingUtils
    {
        public static readonly Encoding Utf16 = new UnicodeEncoding(false, false);
        public static readonly Encoding Utf16be = new UnicodeEncoding(true, false);
        public static readonly Encoding Utf7 = new UTF7Encoding(true);
        public static readonly Encoding Utf8 = new UTF8Encoding(false);
        public static readonly Encoding Utf32 = new UTF32Encoding(false, false);
        public static readonly Encoding Utf32be = new UTF32Encoding(true, false);

        public static Encoding GetEncoding(string name)
        {
            if (name == null)
            {
                return Utf8;
            }

            if (name.Equals("utf-8", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf8;
            }

            if (name.Equals("utf-16", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf16;
            }

            if (name.Equals("utf-7", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf7;
            }

            if (name.Equals("utf-32", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf32;
            }

            if (name.Equals("utf-32be", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf32be;
            }

            if (name.Equals("utf-16be", StringComparison.InvariantCultureIgnoreCase))
            {
                return Utf16be;
            }

            throw new ArgumentException("Invalid encoding name");
        }

        public static string GetEncoding(Encoding name)
        {
            if (name == null)
            {
                return "utf-8";
            }

            if (name.Equals(Utf8))
            {
                return "utf-8";
            }

            if (name.Equals(Utf16))
            {
                return "utf-16";
            }

            if (name.Equals(Utf7))
            {
                return "utf-7";
            }

            if (name.Equals(Utf32))
            {
                return "utf-32";
            }

            if (name.Equals(Utf32be))
            {
                return "utf-32be";
            }

            if (name.Equals(Utf16be))
            {
                return "utf-16be";
            }

            throw new ArgumentException("Invalid encoding");
        }
    }
}
