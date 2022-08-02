// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal;

public class ExpressionException : Exception
{
    public string ExpressionString { get; }

    public int Position { get; set; }

    public override string Message => ToDetailedString();

    public string SimpleMessage => base.Message;

    public ExpressionException(string message)
        : base(message)
    {
        ExpressionString = null;
        Position = 0;
    }

    public ExpressionException(string message, Exception cause)
        : base(message, cause)
    {
        ExpressionString = null;
        Position = 0;
    }

    public ExpressionException(string expressionString, string message)
        : base(message)
    {
        ExpressionString = expressionString;
        Position = -1;
    }

    public ExpressionException(string expressionString, int position, string message)
        : base(message)
    {
        ExpressionString = expressionString;
        Position = position;
    }

    public ExpressionException(int position, string message)
        : base(message)
    {
        ExpressionString = null;
        Position = position;
    }

    public ExpressionException(int position, string message, Exception cause)
        : base(message, cause)
    {
        ExpressionString = null;
        Position = position;
    }

    public string ToDetailedString()
    {
        if (ExpressionString != null)
        {
            var output = new StringBuilder();
            output.Append("Expression [");
            output.Append(ExpressionString);
            output.Append(']');

            if (Position >= 0)
            {
                output.Append(" @");
                output.Append(Position);
            }

            output.Append(": ");
            output.Append(SimpleMessage);
            return output.ToString();
        }

        return SimpleMessage;
    }
}
