// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;
#pragma warning disable S125 // Sections of code should not be commented out

internal sealed class Tokenizer
{
    // If this gets changed, it must remain sorted...
    private static readonly string[] ALTERNATIVE_OPERATOR_NAMES = { "DIV", "EQ", "GE", "GT", "LE", "LT", "MOD", "NE", "NOT" };

    private static readonly byte[] FLAGS = new byte[256];

    private static readonly byte IS_DIGIT = 0x01;

    private static readonly byte IS_HEXDIGIT = 0x02;

    private static readonly byte IS_ALPHA = 0x04;

    static Tokenizer()
    {
        for (int ch = '0'; ch <= '9'; ch++)
        {
            FLAGS[ch] |= (byte)(IS_DIGIT | IS_HEXDIGIT);
        }

        for (int ch = 'A'; ch <= 'F'; ch++)
        {
            FLAGS[ch] |= IS_HEXDIGIT;
        }

        for (int ch = 'a'; ch <= 'f'; ch++)
        {
            FLAGS[ch] |= IS_HEXDIGIT;
        }

        for (int ch = 'A'; ch <= 'Z'; ch++)
        {
            FLAGS[ch] |= IS_ALPHA;
        }

        for (int ch = 'a'; ch <= 'z'; ch++)
        {
            FLAGS[ch] |= IS_ALPHA;
        }
    }

    private readonly int _max;

    private readonly List<Token> _tokens = new ();

    private readonly string _expressionString;

    private readonly char[] _charsToProcess;

    private int _pos;

    public Tokenizer(string inputData)
    {
        _expressionString = inputData;
        _charsToProcess = (inputData + "\0").ToCharArray();
        _max = _charsToProcess.Length;
        _pos = 0;
    }

    public List<Token> Process()
    {
        while (_pos < _max)
        {
            var ch = _charsToProcess[_pos];
            if (IsAlphabetic(ch))
            {
                LexIdentifier();
            }
            else
            {
#pragma warning disable S1479 // "switch" statements should not have too many "case" clauses
                switch (ch)
#pragma warning restore S1479 // "switch" statements should not have too many "case" clauses
                {
                    case '+':
                        if (IsTwoCharToken(TokenKind.INC))
                        {
                            PushPairToken(TokenKind.INC);
                        }
                        else
                        {
                            PushCharToken(TokenKind.PLUS);
                        }

                        break;
                    case '_': // the other way to start an identifier
                        LexIdentifier();
                        break;
                    case '-':
                        if (IsTwoCharToken(TokenKind.DEC))
                        {
                            PushPairToken(TokenKind.DEC);
                        }
                        else
                        {
                            PushCharToken(TokenKind.MINUS);
                        }

                        break;
                    case ':':
                        PushCharToken(TokenKind.COLON);
                        break;
                    case '.':
                        PushCharToken(TokenKind.DOT);
                        break;
                    case ',':
                        PushCharToken(TokenKind.COMMA);
                        break;
                    case '*':
                        PushCharToken(TokenKind.STAR);
                        break;
                    case '/':
                        PushCharToken(TokenKind.DIV);
                        break;
                    case '%':
                        PushCharToken(TokenKind.MOD);
                        break;
                    case '(':
                        PushCharToken(TokenKind.LPAREN);
                        break;
                    case ')':
                        PushCharToken(TokenKind.RPAREN);
                        break;
                    case '[':
                        PushCharToken(TokenKind.LSQUARE);
                        break;
                    case '#':
                        PushCharToken(TokenKind.HASH);
                        break;
                    case ']':
                        PushCharToken(TokenKind.RSQUARE);
                        break;
                    case '{':
                        PushCharToken(TokenKind.LCURLY);
                        break;
                    case '}':
                        PushCharToken(TokenKind.RCURLY);
                        break;
                    case '@':
                        PushCharToken(TokenKind.SERVICE_REF);
                        break;
                    case '^':
                        if (IsTwoCharToken(TokenKind.SELECT_FIRST))
                        {
                            PushPairToken(TokenKind.SELECT_FIRST);
                        }
                        else
                        {
                            PushCharToken(TokenKind.POWER);
                        }

                        break;
                    case '!':
                        if (IsTwoCharToken(TokenKind.NE))
                        {
                            PushPairToken(TokenKind.NE);
                        }
                        else if (IsTwoCharToken(TokenKind.PROJECT))
                        {
                            PushPairToken(TokenKind.PROJECT);
                        }
                        else
                        {
                            PushCharToken(TokenKind.NOT);
                        }

                        break;
                    case '=':
                        if (IsTwoCharToken(TokenKind.EQ))
                        {
                            PushPairToken(TokenKind.EQ);
                        }
                        else
                        {
                            PushCharToken(TokenKind.ASSIGN);
                        }

                        break;
                    case '&':
                        if (IsTwoCharToken(TokenKind.SYMBOLIC_AND))
                        {
                            PushPairToken(TokenKind.SYMBOLIC_AND);
                        }
                        else
                        {
                            PushCharToken(TokenKind.FACTORY_SERVICE_REF);
                        }

                        break;
                    case '|':
                        if (!IsTwoCharToken(TokenKind.SYMBOLIC_OR))
                        {
                            RaiseParseException(_pos, SpelMessage.MISSING_CHARACTER, "|");
                        }

                        PushPairToken(TokenKind.SYMBOLIC_OR);
                        break;
                    case '?':
                        if (IsTwoCharToken(TokenKind.SELECT))
                        {
                            PushPairToken(TokenKind.SELECT);
                        }
                        else if (IsTwoCharToken(TokenKind.ELVIS))
                        {
                            PushPairToken(TokenKind.ELVIS);
                        }
                        else if (IsTwoCharToken(TokenKind.SAFE_NAVI))
                        {
                            PushPairToken(TokenKind.SAFE_NAVI);
                        }
                        else
                        {
                            PushCharToken(TokenKind.QMARK);
                        }

                        break;
                    case '$':
                        if (IsTwoCharToken(TokenKind.SELECT_LAST))
                        {
                            PushPairToken(TokenKind.SELECT_LAST);
                        }
                        else
                        {
                            LexIdentifier();
                        }

                        break;
                    case '>':
                        if (IsTwoCharToken(TokenKind.GE))
                        {
                            PushPairToken(TokenKind.GE);
                        }
                        else
                        {
                            PushCharToken(TokenKind.GT);
                        }

                        break;
                    case '<':
                        if (IsTwoCharToken(TokenKind.LE))
                        {
                            PushPairToken(TokenKind.LE);
                        }
                        else
                        {
                            PushCharToken(TokenKind.LT);
                        }

                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        LexNumericLiteral(ch == '0');
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // drift over white space
                        _pos++;
                        break;
                    case '\'':
                        LexQuotedStringLiteral();
                        break;
                    case '"':
                        LexDoubleQuotedStringLiteral();
                        break;
                    case '\0':
                        // hit sentinel at end of value
                        _pos++;  // will take us to the end
                        break;
                    case '\\':
                        RaiseParseException(_pos, SpelMessage.UNEXPECTED_ESCAPE_CHAR);
                        break;
                    default:
                        throw new InvalidOperationException($"Cannot handle ({(int)ch}) '{ch}'");
                }
            }
        }

        return _tokens;
    }

    // STRING_LITERAL: '\''! (APOS|~'\'')* '\''!;
    private void LexQuotedStringLiteral()
    {
        var start = _pos;
        var terminated = false;
        while (!terminated)
        {
            _pos++;
            var ch = _charsToProcess[_pos];
            if (ch == '\'')
            {
                // may not be the end if the char after is also a '
                if (_charsToProcess[_pos + 1] == '\'')
                {
                    _pos++;  // skip over that too, and continue
                }
                else
                {
                    terminated = true;
                }
            }

            if (IsExhausted())
            {
                RaiseParseException(start, SpelMessage.NON_TERMINATING_QUOTED_STRING);
            }
        }

        _pos++;
        _tokens.Add(new Token(TokenKind.LITERAL_STRING, Subarray(start, _pos), start, _pos));
    }

    // DQ_STRING_LITERAL: '"'! (~'"')* '"'!;
    private void LexDoubleQuotedStringLiteral()
    {
        var start = _pos;
        var terminated = false;
        while (!terminated)
        {
            _pos++;
            var ch = _charsToProcess[_pos];
            if (ch == '"')
            {
                // may not be the end if the char after is also a "
                if (_charsToProcess[_pos + 1] == '"')
                {
                    _pos++;  // skip over that too, and continue
                }
                else
                {
                    terminated = true;
                }
            }

            if (IsExhausted())
            {
                RaiseParseException(start, SpelMessage.NON_TERMINATING_DOUBLE_QUOTED_STRING);
            }
        }

        _pos++;
        _tokens.Add(new Token(TokenKind.LITERAL_STRING, Subarray(start, _pos), start, _pos));
    }

    // REAL_LITERAL :
    // ('.' (DECIMAL_DIGIT)+ (EXPONENT_PART)? (REAL_TYPE_SUFFIX)?) |
    // ((DECIMAL_DIGIT)+ '.' (DECIMAL_DIGIT)+ (EXPONENT_PART)? (REAL_TYPE_SUFFIX)?) |
    // ((DECIMAL_DIGIT)+ (EXPONENT_PART) (REAL_TYPE_SUFFIX)?) |
    // ((DECIMAL_DIGIT)+ (REAL_TYPE_SUFFIX));
    // fragment INTEGER_TYPE_SUFFIX : ( 'L' | 'l' );
    // fragment HEX_DIGIT :
    // '0'|'1'|'2'|'3'|'4'|'5'|'6'|'7'|'8'|'9'|'A'|'B'|'C'|'D'|'E'|'F'|'a'|'b'|'c'|'d'|'e'|'f';
    //
    // fragment EXPONENT_PART : 'e' (SIGN)* (DECIMAL_DIGIT)+ | 'E' (SIGN)*
    // (DECIMAL_DIGIT)+ ;
    // fragment SIGN : '+' | '-' ;
    // fragment REAL_TYPE_SUFFIX : 'F' | 'f' | 'D' | 'd';
    // INTEGER_LITERAL
    // : (DECIMAL_DIGIT)+ (INTEGER_TYPE_SUFFIX)?;
    private void LexNumericLiteral(bool firstCharIsZero)
    {
        var isReal = false;
        var start = _pos;
        var ch = _charsToProcess[_pos + 1];
        var isHex = ch == 'x' || ch == 'X';

        // deal with hexadecimal
        if (firstCharIsZero && isHex)
        {
            _pos++;
            do
            {
                _pos++;
            }
            while (IsHexadecimalDigit(_charsToProcess[_pos]));
            if (IsChar('L', 'l'))
            {
                PushHexIntToken(Subarray(start + 2, _pos), true, start, _pos);
                _pos++;
            }
            else
            {
                PushHexIntToken(Subarray(start + 2, _pos), false, start, _pos);
            }

            return;
        }

        // real numbers must have leading digits

        // Consume first part of number
        do
        {
            _pos++;
        }
        while (IsDigit(_charsToProcess[_pos]));

        // a '.' indicates this number is a real
        ch = _charsToProcess[_pos];
        if (ch == '.')
        {
            isReal = true;
            var dotpos = _pos;

            // carry on consuming digits
            do
            {
                _pos++;
            }
            while (IsDigit(_charsToProcess[_pos]));
            if (_pos == dotpos + 1)
            {
                // the number is something like '3.'. It is really an int but may be
                // part of something like '3.toString()'. In this case process it as
                // an int and leave the dot as a separate token.
                _pos = dotpos;
                PushIntToken(Subarray(start, _pos), false, start, _pos);
                return;
            }
        }

        var endOfNumber = _pos;

        // Now there may or may not be an exponent

        // Is it a long ?
        if (IsChar('L', 'l'))
        {
            if (isReal)
            {
                // 3.4L - not allowed
                RaiseParseException(start, SpelMessage.REAL_CANNOT_BE_LONG);
            }

            PushIntToken(Subarray(start, endOfNumber), true, start, endOfNumber);
            _pos++;
        }
        else if (IsExponentChar(_charsToProcess[_pos]))
        {
            _pos++;
            var possibleSign = _charsToProcess[_pos];
            if (IsSign(possibleSign))
            {
                _pos++;
            }

            // exponent digits
            do
            {
                _pos++;
            }
            while (IsDigit(_charsToProcess[_pos]));
            var isFloat = false;
            if (IsFloatSuffix(_charsToProcess[_pos]))
            {
                isFloat = true;
                ++_pos;
            }
            else if (IsDoubleSuffix(_charsToProcess[_pos]))
            {
                ++_pos;
            }

            PushRealToken(Subarray(start, _pos), isFloat, start, _pos);
        }
        else
        {
            ch = _charsToProcess[_pos];
            var isFloat = false;
            if (IsFloatSuffix(ch))
            {
                isReal = true;
                isFloat = true;
                endOfNumber = ++_pos;
            }
            else if (IsDoubleSuffix(ch))
            {
                isReal = true;
                endOfNumber = ++_pos;
            }

            if (isReal)
            {
                PushRealToken(Subarray(start, endOfNumber), isFloat, start, endOfNumber);
            }
            else
            {
                PushIntToken(Subarray(start, endOfNumber), false, start, endOfNumber);
            }
        }
    }

    private void LexIdentifier()
    {
        var start = _pos;
        do
        {
            _pos++;
        }
        while (IsIdentifier(_charsToProcess[_pos]));
        var subarray = Subarray(start, _pos);

        // Check if this is the alternative (textual) representation of an operator (see
        // alternativeOperatorNames)
        if (_pos - start == 2 || _pos - start == 3)
        {
            var asString = new string(subarray).ToUpper();
            var idx = Array.BinarySearch(ALTERNATIVE_OPERATOR_NAMES, asString);
            if (idx >= 0)
            {
                PushOneCharOrTwoCharToken(TokenKind.ValueOf(asString), start, subarray);
                return;
            }
        }

        _tokens.Add(new Token(TokenKind.IDENTIFIER, subarray, start, _pos));
    }

    private void PushIntToken(char[] data, bool isLong, int start, int end)
    {
        _tokens.Add(isLong
            ? new Token(TokenKind.LITERAL_LONG, data, start, end)
            : new Token(TokenKind.LITERAL_INT, data, start, end));
    }

    private void PushHexIntToken(char[] data, bool isLong, int start, int end)
    {
        if (data.Length == 0)
        {
            if (isLong)
            {
                RaiseParseException(start, SpelMessage.NOT_A_LONG, _expressionString.Substring(start, end + 1 - start));
            }
            else
            {
                RaiseParseException(start, SpelMessage.NOT_AN_INTEGER, _expressionString.Substring(start, end - start));
            }
        }

        _tokens.Add(isLong
            ? new Token(TokenKind.LITERAL_HEXLONG, data, start, end)
            : new Token(TokenKind.LITERAL_HEXINT, data, start, end));
    }

    private void PushRealToken(char[] data, bool isFloat, int start, int end)
    {
        _tokens.Add(isFloat
            ? new Token(TokenKind.LITERAL_REAL_FLOAT, data, start, end)
            : new Token(TokenKind.LITERAL_REAL, data, start, end));
    }

    private char[] Subarray(int start, int end)
    {
        var result = new char[end - start];
        Array.Copy(_charsToProcess, start, result, 0, end - start);
        return result;
    }

    private bool IsTwoCharToken(TokenKind kind)
    {
        return kind.TokenChars.Length == 2 &&
               _charsToProcess[_pos] == kind.TokenChars[0] &&
               _charsToProcess[_pos + 1] == kind.TokenChars[1];
    }

    private void PushCharToken(TokenKind kind)
    {
        _tokens.Add(new Token(kind, _pos, _pos + 1));
        _pos++;
    }

    private void PushPairToken(TokenKind kind)
    {
        _tokens.Add(new Token(kind, _pos, _pos + 2));
        _pos += 2;
    }

    private void PushOneCharOrTwoCharToken(TokenKind kind, int pos, char[] data)
    {
        _tokens.Add(new Token(kind, data, pos, pos + kind.Length));
    }

    // ID: ('a'..'z'|'A'..'Z'|'_'|'$') ('a'..'z'|'A'..'Z'|'_'|'$'|'0'..'9'|DOT_ESCAPED)*;
    private bool IsIdentifier(char ch)
    {
        return IsAlphabetic(ch) || IsDigit(ch) || ch == '_' || ch == '$';
    }

    private bool IsChar(char a, char b)
    {
        var ch = _charsToProcess[_pos];
        return ch == a || ch == b;
    }

    private bool IsExponentChar(char ch)
    {
        return ch == 'e' || ch == 'E';
    }

    private bool IsFloatSuffix(char ch)
    {
        return ch == 'f' || ch == 'F';
    }

    private bool IsDoubleSuffix(char ch)
    {
        return ch == 'd' || ch == 'D';
    }

    private bool IsSign(char ch)
    {
        return ch == '+' || ch == '-';
    }

    private bool IsDigit(char ch)
    {
        if (ch > 255)
        {
            return false;
        }

        return (FLAGS[ch] & IS_DIGIT) != 0;
    }

    private bool IsAlphabetic(char ch)
    {
        if (ch > 255)
        {
            return false;
        }

        return (FLAGS[ch] & IS_ALPHA) != 0;
    }

    private bool IsHexadecimalDigit(char ch)
    {
        if (ch > 255)
        {
            return false;
        }

        return (FLAGS[ch] & IS_HEXDIGIT) != 0;
    }

    private bool IsExhausted()
    {
        return _pos == _max - 1;
    }

    private void RaiseParseException(int start, SpelMessage msg, params object[] inserts)
    {
        throw new InternalParseException(new SpelParseException(_expressionString, start, msg, inserts));
    }
}
#pragma warning restore S125 // Sections of code should not be commented out
