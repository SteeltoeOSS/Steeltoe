// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal;

public class ParseException : ExpressionException
{
    public ParseException(string expressionString, int position, string message)
        : base(expressionString, position, message)
    {
    }

    public ParseException(int position, string message, Exception cause)
        : base(position, message, cause)
    {
    }

    public ParseException(int position, string message)
        : base(position, message)
    {
    }
}