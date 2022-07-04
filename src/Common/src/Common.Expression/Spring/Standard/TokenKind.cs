// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class TokenKind
{
    private static readonly Dictionary<string, TokenKind> Kinds = new ();

    // ordered by priority - operands first
    // Adding anything in this list requires adjusting order parameter
    public static readonly TokenKind LiteralInt = new (1, "LITERAL_INT");
    public static readonly TokenKind LiteralLong = new (2, "LITERAL_LONG");
    public static readonly TokenKind LiteralHexint = new (3, "LITERAL_HEXINT");
    public static readonly TokenKind LiteralHexlong = new (4, "LITERAL_HEXLONG");
    public static readonly TokenKind LiteralString = new (5, "LITERAL_STRING");
    public static readonly TokenKind LiteralReal = new (6, "LITERAL_REAL");
    public static readonly TokenKind LiteralRealFloat = new (7, "LITERAL_REAL_FLOAT");
    public static readonly TokenKind Lparen = new (8, "LPAREN", "(");
    public static readonly TokenKind Rparen = new (9, "RPAREN", ")");
    public static readonly TokenKind Comma = new (10, "COMMA", ",");
    public static readonly TokenKind Identifier = new (11, "IDENTIFIER");
    public static readonly TokenKind Colon = new (12, "COLON", ":");
    public static readonly TokenKind Hash = new (13, "HASH", "#");
    public static readonly TokenKind Rsquare = new (14, "RSQUARE", "]");
    public static readonly TokenKind Lsquare = new (15, "LSQUARE", "[");
    public static readonly TokenKind Lcurly = new (16, "LCURLY", "{");
    public static readonly TokenKind Rcurly = new (17, "RCURLY", "}");
    public static readonly TokenKind Dot = new (18, "DOT", ".");
    public static readonly TokenKind Plus = new (19, "PLUS", "+");
    public static readonly TokenKind Star = new (20, "STAR", "*");
    public static readonly TokenKind Minus = new (21, "MINUS", "-");
    public static readonly TokenKind SelectFirst = new (22, "SELECT_FIRST", "^[");
    public static readonly TokenKind SelectLast = new (23, "SELECT_LAST", "$[");
    public static readonly TokenKind Qmark = new (24, "QMARK", "?");
    public static readonly TokenKind Project = new (25, "PROJECT", "![");
    public static readonly TokenKind Div = new (26, "DIV", "/");
    public static readonly TokenKind Ge = new (27, "GE", ">=");
    public static readonly TokenKind Gt = new (28, "GT", ">");
    public static readonly TokenKind Le = new (29, "LE", "<=");
    public static readonly TokenKind Lt = new (30, "LT", "<");
    public static readonly TokenKind Eq = new (31, "EQ", "==");
    public static readonly TokenKind Ne = new (32, "NE", "!=");
    public static readonly TokenKind Mod = new (33, "MOD", "%");
    public static readonly TokenKind Not = new (34, "NOT", "!");
    public static readonly TokenKind Assign = new (35, "ASSIGN", "=");
    public static readonly TokenKind Instanceof = new (36, "INSTANCEOF", "instanceof");
    public static readonly TokenKind Matches = new (37, "MATCHES", "matches");
    public static readonly TokenKind Between = new (38, "BETWEEN", "between");
    public static readonly TokenKind Select = new (39, "SELECT", "?[");
    public static readonly TokenKind Power = new (40, "POWER", "^");
    public static readonly TokenKind Elvis = new (41, "ELVIS", "?:");
    public static readonly TokenKind SafeNavi = new (42, "SAFE_NAVI", "?.");
    public static readonly TokenKind ServiceRef = new (43, "SERVICE_REF", "@");
    public static readonly TokenKind FactoryServiceRef = new (44, "FACTORY_SERVICE_REF", "&");
    public static readonly TokenKind SymbolicOr = new (45, "SYMBOLIC_OR", "||");
    public static readonly TokenKind SymbolicAnd = new (46, "SYMBOLIC_AND", "&&");
    public static readonly TokenKind Inc = new (47, "INC", "++");
    public static readonly TokenKind Dec = new (48, "DEC", "--");

    private TokenKind(int order, string name, string tokenString)
    {
        Ordinal = order;
        Name = name;
        TokenChars = tokenString?.ToCharArray();
        HasPayload = TokenChars?.Length == 0;
        Kinds.Add(Name, this);
    }

    private TokenKind(int order, string name)
        : this(order, name, string.Empty)
    {
    }

    public bool HasPayload { get; }

    public int Length => TokenChars.Length;

    public string Name { get; }

    public char[] TokenChars { get; }

    public int Ordinal { get; }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not TokenKind other)
        {
            return false;
        }

        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public override string ToString()
    {
        return Name + (TokenChars.Length != 0 ? $"({new string(TokenChars)})" : string.Empty);
    }

    internal static TokenKind ValueOf(string name)
    {
        if (!Kinds.TryGetValue(name, out var kind))
        {
            throw new InvalidOperationException($"Invalid TokenKind name:  {name}");
        }

        return kind;
    }
}
