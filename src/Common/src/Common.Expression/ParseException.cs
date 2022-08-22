// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal;

public class ParseException : ExpressionException
{
    public ParseException(int position, string message)
        : base(position, message)
    {
    }

    public ParseException(int position, string message, Exception innerException)
        : base(position, message, innerException)
    {
    }

    public ParseException(string expressionString, int position, string message)
        : base(expressionString, position, message)
    {
    }
}
