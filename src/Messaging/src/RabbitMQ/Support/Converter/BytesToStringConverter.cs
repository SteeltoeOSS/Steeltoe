﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter
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
