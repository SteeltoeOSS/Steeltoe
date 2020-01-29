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
    }
}
