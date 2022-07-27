// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class SpelParseException : ParseException
{
    public SpelMessage MessageCode { get; }

    public object[] Inserts { get; }

    public SpelParseException(string expressionString, int position, SpelMessage message, params object[] inserts)
        : base(expressionString, position, message.FormatMessage(inserts))
    {
        MessageCode = message;
        Inserts = inserts;
    }

    public SpelParseException(int position, SpelMessage message, params object[] inserts)
        : base(position, message.FormatMessage(inserts))
    {
        MessageCode = message;
        Inserts = inserts;
    }

    public SpelParseException(int position, Exception cause, SpelMessage message, params object[] inserts)
        : base(position, message.FormatMessage(inserts), cause)
    {
        MessageCode = message;
        Inserts = inserts;
    }
}