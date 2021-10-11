// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class TokenKind
    {
#pragma warning disable S3963 // "static" fields should be initialized inline
        static TokenKind()
        {
            _kinds = new Dictionary<string, TokenKind>();
            LITERAL_INT = new TokenKind(1, "LITERAL_INT");
            LITERAL_LONG = new TokenKind(2, "LITERAL_LONG");
            LITERAL_HEXINT = new TokenKind(3, "LITERAL_HEXINT");
            LITERAL_HEXLONG = new TokenKind(4, "LITERAL_HEXLONG");
            LITERAL_STRING = new TokenKind(5, "LITERAL_STRING");
            LITERAL_REAL = new TokenKind(6, "LITERAL_REAL");
            LITERAL_REAL_FLOAT = new TokenKind(7, "LITERAL_REAL_FLOAT");
            LPAREN = new TokenKind(8, "LPAREN", "(");
            RPAREN = new TokenKind(9, "RPAREN", ")");
            COMMA = new TokenKind(10, "COMMA", ",");
            IDENTIFIER = new TokenKind(11, "IDENTIFIER");
            COLON = new TokenKind(12, "COLON", ":");
            HASH = new TokenKind(13, "HASH", "#");
            RSQUARE = new TokenKind(14, "RSQUARE", "]");
            LSQUARE = new TokenKind(15, "LSQUARE", "[");
            LCURLY = new TokenKind(16, "LCURLY", "{");
            RCURLY = new TokenKind(17, "RCURLY", "}");
            DOT = new TokenKind(18, "DOT", ".");
            PLUS = new TokenKind(19, "PLUS", "+");
            STAR = new TokenKind(20, "STAR", "*");
            MINUS = new TokenKind(21, "MINUS", "-");
            SELECT_FIRST = new TokenKind(22, "SELECT_FIRST", "^[");
            SELECT_LAST = new TokenKind(23, "SELECT_LAST", "$[");
            QMARK = new TokenKind(24, "QMARK", "?");
            PROJECT = new TokenKind(25, "PROJECT", "![");
            DIV = new TokenKind(26, "DIV", "/");
            GE = new TokenKind(27, "GE", ">=");
            GT = new TokenKind(28, "GT", ">");
            LE = new TokenKind(29, "LE", "<=");
            LT = new TokenKind(30, "LT", "<");
            EQ = new TokenKind(31, "EQ", "==");
            NE = new TokenKind(32, "NE", "!=");
            MOD = new TokenKind(33, "MOD", "%");
            NOT = new TokenKind(34, "NOT", "!");
            ASSIGN = new TokenKind(35, "ASSIGN", "=");
            INSTANCEOF = new TokenKind(36, "INSTANCEOF", "instanceof");
            MATCHES = new TokenKind(37, "MATCHES", "matches");
            BETWEEN = new TokenKind(38, "BETWEEN", "between");
            SELECT = new TokenKind(39, "SELECT", "?[");
            POWER = new TokenKind(40, "POWER", "^");
            ELVIS = new TokenKind(41, "ELVIS", "?:");
            SAFE_NAVI = new TokenKind(42, "SAFE_NAVI", "?.");
            SERVICE_REF = new TokenKind(43, "SERVICE_REF", "@");
            FACTORY_SERVICE_REF = new TokenKind(44, "FACTORY_SERVICE_REF", "&");
            SYMBOLIC_OR = new TokenKind(45, "SYMBOLIC_OR", "||");
            SYMBOLIC_AND = new TokenKind(46, "SYMBOLIC_AND", "&&");
            INC = new TokenKind(47, "INC", "++");
            DEC = new TokenKind(48, "DEC", "--");
        }
#pragma warning restore S3963 // "static" fields should be initialized inline

        // ordered by priority - operands first
        // When adding anything in this list requires adjusting order parameter
        public static readonly TokenKind LITERAL_INT;
        public static readonly TokenKind LITERAL_LONG;
        public static readonly TokenKind LITERAL_HEXINT;
        public static readonly TokenKind LITERAL_HEXLONG;
        public static readonly TokenKind LITERAL_STRING;
        public static readonly TokenKind LITERAL_REAL;
        public static readonly TokenKind LITERAL_REAL_FLOAT;
        public static readonly TokenKind LPAREN;
        public static readonly TokenKind RPAREN;
        public static readonly TokenKind COMMA;
        public static readonly TokenKind IDENTIFIER;
        public static readonly TokenKind COLON;
        public static readonly TokenKind HASH;
        public static readonly TokenKind RSQUARE;
        public static readonly TokenKind LSQUARE;
        public static readonly TokenKind LCURLY;
        public static readonly TokenKind RCURLY;
        public static readonly TokenKind DOT;
        public static readonly TokenKind PLUS;
        public static readonly TokenKind STAR;
        public static readonly TokenKind MINUS;
        public static readonly TokenKind SELECT_FIRST;
        public static readonly TokenKind SELECT_LAST;
        public static readonly TokenKind QMARK;
        public static readonly TokenKind PROJECT;
        public static readonly TokenKind DIV;
        public static readonly TokenKind GE;
        public static readonly TokenKind GT;
        public static readonly TokenKind LE;
        public static readonly TokenKind LT;
        public static readonly TokenKind EQ;
        public static readonly TokenKind NE;
        public static readonly TokenKind MOD;
        public static readonly TokenKind NOT;
        public static readonly TokenKind ASSIGN;
        public static readonly TokenKind INSTANCEOF;
        public static readonly TokenKind MATCHES;
        public static readonly TokenKind BETWEEN;
        public static readonly TokenKind SELECT;
        public static readonly TokenKind POWER;
        public static readonly TokenKind ELVIS;
        public static readonly TokenKind SAFE_NAVI;
        public static readonly TokenKind SERVICE_REF;
        public static readonly TokenKind FACTORY_SERVICE_REF;
        public static readonly TokenKind SYMBOLIC_OR;
        public static readonly TokenKind SYMBOLIC_AND;
        public static readonly TokenKind INC;
        public static readonly TokenKind DEC;

        private static readonly Dictionary<string, TokenKind> _kinds;

        private readonly char[] _tokenChars;

        private readonly bool _hasPayload;  // is there more to this token than simply the kind

        private readonly string _name;

        private readonly int _order;

        private TokenKind(int order, string name, string tokenString)
        {
            _order = order;
            _name = name;
            _tokenChars = tokenString?.ToCharArray();
            _hasPayload = _tokenChars?.Length == 0;
            _kinds.Add(_name, this);
        }

        private TokenKind(int order, string name)
            : this(order, name, string.Empty)
        {
        }

        public bool HasPayload => _hasPayload;

        public int Length => _tokenChars.Length;

        public string Name => _name;

        public char[] TokenChars => _tokenChars;

        public int Ordinal => _order;

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
            return Name + (_tokenChars.Length != 0 ? "(" + new string(_tokenChars) + ")" : string.Empty);
        }

        internal static TokenKind ValueOf(string name)
        {
            if (!_kinds.TryGetValue(name, out var kind))
            {
                throw new InvalidOperationException("Invalid TokenKind name:  " + name);
            }

            return kind;
        }
    }
}
