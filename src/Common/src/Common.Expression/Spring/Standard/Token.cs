// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class Token
{
    private static readonly ISet<TokenKind> NumericOperatorKinds = new[]
    {
        TokenKind.Gt,
        TokenKind.Ge,
        TokenKind.Lt,
        TokenKind.Le,
        TokenKind.Eq,
        TokenKind.Ne
    }.ToHashSet();

    public TokenKind Kind { get; }

    public int StartPos { get; }

    public int EndPos { get; }

    public string Data { get; }

    public bool IsIdentifier => Equals(Kind, TokenKind.Identifier);

    public bool IsNumericRelationalOperator => NumericOperatorKinds.Contains(Kind);

    public string StringValue => Data ?? string.Empty;

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

    public Token AsInstanceOfToken()
    {
        return new Token(TokenKind.InstanceOf, StartPos, EndPos);
    }

    public Token AsMatchesToken()
    {
        return new Token(TokenKind.Matches, StartPos, EndPos);
    }

    public Token AsBetweenToken()
    {
        return new Token(TokenKind.Between, StartPos, EndPos);
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
