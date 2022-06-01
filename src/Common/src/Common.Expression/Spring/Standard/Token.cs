// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class Token
{
    public Token(TokenKind tokenKind, int startPos, int endPos)
    {
        Kind = tokenKind;
        StartPos = startPos;
        EndPos = endPos;
    }

    public Token(TokenKind tokenKind, char[] tokenData, int startPos, int endPos)
        : this(tokenKind, startPos, endPos)
    {
        Data = new string(tokenData);
    }

    public TokenKind Kind { get; }

    public int StartPos { get; }

    public int EndPos { get; }

    public string Data { get; }

    public bool IsIdentifier
    {
        get { return Kind == TokenKind.IDENTIFIER; }
    }

    public bool IsNumericRelationalOperator
    {
        get
        {
            return Kind == TokenKind.GT || Kind == TokenKind.GE || Kind == TokenKind.LT ||
                   Kind == TokenKind.LE || Kind == TokenKind.EQ || Kind == TokenKind.NE;
        }
    }

    public string StringValue
    {
        get { return Data ?? string.Empty; }
    }

    public Token AsInstanceOfToken()
    {
        return new Token(TokenKind.INSTANCEOF, StartPos, EndPos);
    }

    public Token AsMatchesToken()
    {
        return new Token(TokenKind.MATCHES, StartPos, EndPos);
    }

    public Token AsBetweenToken()
    {
        return new Token(TokenKind.BETWEEN, StartPos, EndPos);
    }

    public override string ToString()
    {
        var s = new StringBuilder();
        s.Append('[').Append(Kind);
        if (Kind.HasPayload)
        {
            s.Append(':').Append(Data);
        }

        s.Append(']');
        s.Append('(').Append(StartPos).Append(',').Append(EndPos).Append(')');
        return s.ToString();
    }
}
