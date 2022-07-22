// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Converter;

public class EncodingToStringConverter : AbstractConverter<Encoding, string>
{
    public override string Convert(Encoding source)
    {
        return source.BodyName;
    }
}