// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class TokenKind
{
    private static readonly Dictionary<string, TokenKind> _kinds = new ();

    // ordered by priority - operands first
    // Adding anything in this list requires adjusting order parameter
    public static readonly TokenKind LITERAL_INT = new (1, "LITERAL_INT");
    public static readonly TokenKind LITERAL_LONG = new (2, "LITERAL_LONG");
    public static readonly TokenKind LITERAL_HEXINT = new (3, "LITERAL_HEXINT");
    public static readonly TokenKind LITERAL_HEXLONG = new (4, "LITERAL_HEXLONG");
    public static readonly TokenKind LITERAL_STRING = new (5, "LITERAL_STRING");
    public static readonly TokenKind LITERAL_REAL = new (6, "LITERAL_REAL");
    public static readonly TokenKind LITERAL_REAL_FLOAT = new (7, "LITERAL_REAL_FLOAT");
    public static readonly TokenKind LPAREN = new (8, "LPAREN", "(");
    public static readonly TokenKind RPAREN = new (9, "RPAREN", ")");
    public static readonly TokenKind COMMA = new (10, "COMMA", ",");
    public static readonly TokenKind IDENTIFIER = new (11, "IDENTIFIER");
    public static readonly TokenKind COLON = new (12, "COLON", ":");
    public static readonly TokenKind HASH = new (13, "HASH", "#");
    public static readonly TokenKind RSQUARE = new (14, "RSQUARE", "]");
    public static readonly TokenKind LSQUARE = new (15, "LSQUARE", "[");
    public static readonly TokenKind LCURLY = new (16, "LCURLY", "{");
    public static readonly TokenKind RCURLY = new (17, "RCURLY", "}");
    public static readonly TokenKind DOT = new (18, "DOT", ".");
    public static readonly TokenKind PLUS = new (19, "PLUS", "+");
    public static readonly TokenKind STAR = new (20, "STAR", "*");
    public static readonly TokenKind MINUS = new (21, "MINUS", "-");
    public static readonly TokenKind SELECT_FIRST = new (22, "SELECT_FIRST", "^[");
    public static readonly TokenKind SELECT_LAST = new (23, "SELECT_LAST", "$[");
    public static readonly TokenKind QMARK = new (24, "QMARK", "?");
    public static readonly TokenKind PROJECT = new (25, "PROJECT", "![");
    public static readonly TokenKind DIV = new (26, "DIV", "/");
    public static readonly TokenKind GE = new (27, "GE", ">=");
    public static readonly TokenKind GT = new (28, "GT", ">");
    public static readonly TokenKind LE = new (29, "LE", "<=");
    public static readonly TokenKind LT = new (30, "LT", "<");
    public static readonly TokenKind EQ = new (31, "EQ", "==");
    public static readonly TokenKind NE = new (32, "NE", "!=");
    public static readonly TokenKind MOD = new (33, "MOD", "%");
    public static readonly TokenKind NOT = new (34, "NOT", "!");
    public static readonly TokenKind ASSIGN = new (35, "ASSIGN", "=");
    public static readonly TokenKind INSTANCEOF = new (36, "INSTANCEOF", "instanceof");
    public static readonly TokenKind MATCHES = new (37, "MATCHES", "matches");
    public static readonly TokenKind BETWEEN = new (38, "BETWEEN", "between");
    public static readonly TokenKind SELECT = new (39, "SELECT", "?[");
    public static readonly TokenKind POWER = new (40, "POWER", "^");
    public static readonly TokenKind ELVIS = new (41, "ELVIS", "?:");
    public static readonly TokenKind SAFE_NAVI = new (42, "SAFE_NAVI", "?.");
    public static readonly TokenKind SERVICE_REF = new (43, "SERVICE_REF", "@");
    public static readonly TokenKind FACTORY_SERVICE_REF = new (44, "FACTORY_SERVICE_REF", "&");
    public static readonly TokenKind SYMBOLIC_OR = new (45, "SYMBOLIC_OR", "||");
    public static readonly TokenKind SYMBOLIC_AND = new (46, "SYMBOLIC_AND", "&&");
    public static readonly TokenKind INC = new (47, "INC", "++");
    public static readonly TokenKind DEC = new (48, "DEC", "--");

    private TokenKind(int order, string name, string tokenString)
    {
        Ordinal = order;
        Name = name;
        TokenChars = tokenString?.ToCharArray();
        HasPayload = TokenChars?.Length == 0;
        _kinds.Add(Name, this);
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
        if (!_kinds.TryGetValue(name, out var kind))
        {
            throw new InvalidOperationException($"Invalid TokenKind name:  {name}");
        }

        return kind;
    }
}
