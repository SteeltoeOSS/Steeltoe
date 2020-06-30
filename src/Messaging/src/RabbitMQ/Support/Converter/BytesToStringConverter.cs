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

using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.Rabbit.Support.Converter
{
    public class BytesToStringConverter : IGenericConverter
    {
        private readonly Encoding _charset;

        public BytesToStringConverter(Encoding charset)
        {
            _charset = charset ?? EncodingUtils.Utf8;
            ConvertibleTypes = new HashSet<(Type Source, Type Target)>() { (typeof(byte[]), typeof(string)) };
        }

        public ISet<(Type Source, Type Target)> ConvertibleTypes { get; }

        public object Convert(object source, Type sourceType, Type targetType)
        {
            if (!(source is byte[] asByteArray))
            {
                return null;
            }

            return _charset.GetString(asByteArray);
        }
    }
}
