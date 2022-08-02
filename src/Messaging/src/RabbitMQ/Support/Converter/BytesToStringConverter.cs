// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.RabbitMQ.Support.Converter;

public class BytesToStringConverter : IGenericConverter
{
    private readonly Encoding _charset;

    public ISet<(Type Source, Type Target)> ConvertibleTypes { get; }

    public BytesToStringConverter(Encoding charset)
    {
        _charset = charset ?? EncodingUtils.Utf8;

        ConvertibleTypes = new HashSet<(Type Source, Type Target)>
        {
            (typeof(byte[]), typeof(string))
        };
    }

    public object Convert(object source, Type sourceType, Type targetType)
    {
        return source is not byte[] asByteArray ? null : _charset.GetString(asByteArray);
    }
}
