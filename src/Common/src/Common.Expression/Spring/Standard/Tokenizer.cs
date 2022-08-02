// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;
#pragma warning disable S125 // Sections of code should not be commented out

internal sealed class Tokenizer
{
    // If this gets changed, it must remain sorted...
    private static readonly string[] AlternativeOperatorNames =
    {
        "DIV",
        "EQ",
        "GE",
        "GT",
        "LE",
        "LT",
        "MOD",
        "NE",
        "NOT"
    };

    private static readonly byte[] Flags = new byte[256];

    private static readonly byte IsDigitFlag = 0x01;

    private static readonly byte IsHexDigitFlag = 0x02;

    private static readonly byte IsAlphaFlag = 0x04;

    private readonly int _max;

    private readonly List<Token> _tokens = new();

    private readonly string _expressionString;

    private readonly char[] _charsToProcess;

    private int _pos;

    static Tokenizer()
    {
        for (int ch = '0'; ch <= '9'; ch++)
        {
            Flags[ch] |= (byte)(IsDigitFlag | IsHexDigitFlag);
        }

        for (int ch = 'A'; ch <= 'F'; ch++)
        {
            Flags[ch] |= IsHexDigitFlag;
        }

        for (int ch = 'a'; ch <= 'f'; ch++)
        {
            Flags[ch] |= IsHexDigitFlag;
        }

        for (int ch = 'A'; ch <= 'Z'; ch++)
        {
            Flags[ch] |= IsAlphaFlag;
        }

        for (int ch = 'a'; ch <= 'z'; ch++)
        {
            Flags[ch] |= IsAlphaFlag;
        }
    }

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
            char ch = _charsToProcess[_pos];

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
                        if (IsTwoCharToken(TokenKind.Inc))
                        {
                            PushPairToken(TokenKind.Inc);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Plus);
                        }

                        break;
                    case '_': // the other way to start an identifier
                        LexIdentifier();
                        break;
                    case '-':
                        if (IsTwoCharToken(TokenKind.Dec))
                        {
                            PushPairToken(TokenKind.Dec);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Minus);
                        }

                        break;
                    case ':':
                        PushCharToken(TokenKind.Colon);
                        break;
                    case '.':
                        PushCharToken(TokenKind.Dot);
                        break;
                    case ',':
                        PushCharToken(TokenKind.Comma);
                        break;
                    case '*':
                        PushCharToken(TokenKind.Star);
                        break;
                    case '/':
                        PushCharToken(TokenKind.Div);
                        break;
                    case '%':
                        PushCharToken(TokenKind.Mod);
                        break;
                    case '(':
                        PushCharToken(TokenKind.LeftParen);
                        break;
                    case ')':
                        PushCharToken(TokenKind.RightParen);
                        break;
                    case '[':
                        PushCharToken(TokenKind.LeftSquare);
                        break;
                    case '#':
                        PushCharToken(TokenKind.Hash);
                        break;
                    case ']':
                        PushCharToken(TokenKind.RightSquare);
                        break;
                    case '{':
                        PushCharToken(TokenKind.LeftCurly);
                        break;
                    case '}':
                        PushCharToken(TokenKind.RightCurly);
                        break;
                    case '@':
                        PushCharToken(TokenKind.ServiceRef);
                        break;
                    case '^':
                        if (IsTwoCharToken(TokenKind.SelectFirst))
                        {
                            PushPairToken(TokenKind.SelectFirst);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Power);
                        }

                        break;
                    case '!':
                        if (IsTwoCharToken(TokenKind.Ne))
                        {
                            PushPairToken(TokenKind.Ne);
                        }
                        else if (IsTwoCharToken(TokenKind.Project))
                        {
                            PushPairToken(TokenKind.Project);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Not);
                        }

                        break;
                    case '=':
                        if (IsTwoCharToken(TokenKind.Eq))
                        {
                            PushPairToken(TokenKind.Eq);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Assign);
                        }

                        break;
                    case '&':
                        if (IsTwoCharToken(TokenKind.SymbolicAnd))
                        {
                            PushPairToken(TokenKind.SymbolicAnd);
                        }
                        else
                        {
                            PushCharToken(TokenKind.FactoryServiceRef);
                        }

                        break;
                    case '|':
                        if (!IsTwoCharToken(TokenKind.SymbolicOr))
                        {
                            RaiseParseException(_pos, SpelMessage.MissingCharacter, "|");
                        }

                        PushPairToken(TokenKind.SymbolicOr);
                        break;
                    case '?':
                        if (IsTwoCharToken(TokenKind.Select))
                        {
                            PushPairToken(TokenKind.Select);
                        }
                        else if (IsTwoCharToken(TokenKind.Elvis))
                        {
                            PushPairToken(TokenKind.Elvis);
                        }
                        else if (IsTwoCharToken(TokenKind.SafeNavigator))
                        {
                            PushPairToken(TokenKind.SafeNavigator);
                        }
                        else
                        {
                            PushCharToken(TokenKind.QuestionMark);
                        }

                        break;
                    case '$':
                        if (IsTwoCharToken(TokenKind.SelectLast))
                        {
                            PushPairToken(TokenKind.SelectLast);
                        }
                        else
                        {
                            LexIdentifier();
                        }

                        break;
                    case '>':
                        if (IsTwoCharToken(TokenKind.Ge))
                        {
                            PushPairToken(TokenKind.Ge);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Gt);
                        }

                        break;
                    case '<':
                        if (IsTwoCharToken(TokenKind.Le))
                        {
                            PushPairToken(TokenKind.Le);
                        }
                        else
                        {
                            PushCharToken(TokenKind.Lt);
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
                        _pos++; // will take us to the end
                        break;
                    case '\\':
                        RaiseParseException(_pos, SpelMessage.UnexpectedEscapeChar);
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
        int start = _pos;
        bool terminated = false;

        while (!terminated)
        {
            _pos++;
            char ch = _charsToProcess[_pos];

            if (ch == '\'')
            {
                // may not be the end if the char after is also a '
                if (_charsToProcess[_pos + 1] == '\'')
                {
                    _pos++; // skip over that too, and continue
                }
                else
                {
                    terminated = true;
                }
            }

            if (IsExhausted())
            {
                RaiseParseException(start, SpelMessage.NonTerminatingQuotedString);
            }
        }

        _pos++;
        _tokens.Add(new Token(TokenKind.LiteralString, SubArray(start, _pos), start, _pos));
    }

    // DQ_STRING_LITERAL: '"'! (~'"')* '"'!;
    private void LexDoubleQuotedStringLiteral()
    {
        int start = _pos;
        bool terminated = false;

        while (!terminated)
        {
            _pos++;
            char ch = _charsToProcess[_pos];

            if (ch == '"')
            {
                // may not be the end if the char after is also a "
                if (_charsToProcess[_pos + 1] == '"')
                {
                    _pos++; // skip over that too, and continue
                }
                else
                {
                    terminated = true;
                }
            }

            if (IsExhausted())
            {
                RaiseParseException(start, SpelMessage.NonTerminatingDoubleQuotedString);
            }
        }

        _pos++;
        _tokens.Add(new Token(TokenKind.LiteralString, SubArray(start, _pos), start, _pos));
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
        bool isReal = false;
        int start = _pos;
        char ch = _charsToProcess[_pos + 1];
        bool isHex = ch == 'x' || ch == 'X';

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
                PushHexIntToken(SubArray(start + 2, _pos), true, start, _pos);
                _pos++;
            }
            else
            {
                PushHexIntToken(SubArray(start + 2, _pos), false, start, _pos);
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
            int dotPos = _pos;

            // carry on consuming digits
            do
            {
                _pos++;
            }
            while (IsDigit(_charsToProcess[_pos]));

            if (_pos == dotPos + 1)
            {
                // the number is something like '3.'. It is really an int but may be
                // part of something like '3.toString()'. In this case process it as
                // an int and leave the dot as a separate token.
                _pos = dotPos;
                PushIntToken(SubArray(start, _pos), false, start, _pos);
                return;
            }
        }

        int endOfNumber = _pos;

        // Now there may or may not be an exponent

        // Is it a long ?
        if (IsChar('L', 'l'))
        {
            if (isReal)
            {
                // 3.4L - not allowed
                RaiseParseException(start, SpelMessage.RealCannotBeLong);
            }

            PushIntToken(SubArray(start, endOfNumber), true, start, endOfNumber);
            _pos++;
        }
        else if (IsExponentChar(_charsToProcess[_pos]))
        {
            _pos++;
            char possibleSign = _charsToProcess[_pos];

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

            bool isFloat = false;

            if (IsFloatSuffix(_charsToProcess[_pos]))
            {
                isFloat = true;
                ++_pos;
            }
            else if (IsDoubleSuffix(_charsToProcess[_pos]))
            {
                ++_pos;
            }

            PushRealToken(SubArray(start, _pos), isFloat, start, _pos);
        }
        else
        {
            ch = _charsToProcess[_pos];
            bool isFloat = false;

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
                PushRealToken(SubArray(start, endOfNumber), isFloat, start, endOfNumber);
            }
            else
            {
                PushIntToken(SubArray(start, endOfNumber), false, start, endOfNumber);
            }
        }
    }

    private void LexIdentifier()
    {
        int start = _pos;

        do
        {
            _pos++;
        }
        while (IsIdentifier(_charsToProcess[_pos]));

        char[] subArray = SubArray(start, _pos);

        // Check if this is the alternative (textual) representation of an operator (see
        // alternativeOperatorNames)
        if (_pos - start == 2 || _pos - start == 3)
        {
            string asString = new string(subArray).ToUpper();
            int idx = Array.BinarySearch(AlternativeOperatorNames, asString);

            if (idx >= 0)
            {
                PushOneCharOrTwoCharToken(TokenKind.ValueOf(asString), start, subArray);
                return;
            }
        }

        _tokens.Add(new Token(TokenKind.Identifier, subArray, start, _pos));
    }

    private void PushIntToken(char[] data, bool isLong, int start, int end)
    {
        _tokens.Add(isLong ? new Token(TokenKind.LiteralLong, data, start, end) : new Token(TokenKind.LiteralInt, data, start, end));
    }

    private void PushHexIntToken(char[] data, bool isLong, int start, int end)
    {
        if (data.Length == 0)
        {
            if (isLong)
            {
                RaiseParseException(start, SpelMessage.NotALong, _expressionString.Substring(start, end + 1 - start));
            }
            else
            {
                RaiseParseException(start, SpelMessage.NotAnInteger, _expressionString.Substring(start, end - start));
            }
        }

        _tokens.Add(isLong ? new Token(TokenKind.LiteralHexLong, data, start, end) : new Token(TokenKind.LiteralHexInt, data, start, end));
    }

    private void PushRealToken(char[] data, bool isFloat, int start, int end)
    {
        _tokens.Add(isFloat ? new Token(TokenKind.LiteralRealFloat, data, start, end) : new Token(TokenKind.LiteralReal, data, start, end));
    }

    private char[] SubArray(int start, int end)
    {
        char[] result = new char[end - start];
        Array.Copy(_charsToProcess, start, result, 0, end - start);
        return result;
    }

    private bool IsTwoCharToken(TokenKind kind)
    {
        return kind.TokenChars.Length == 2 && _charsToProcess[_pos] == kind.TokenChars[0] && _charsToProcess[_pos + 1] == kind.TokenChars[1];
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
        char ch = _charsToProcess[_pos];
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

        return (Flags[ch] & IsDigitFlag) != 0;
    }

    private bool IsAlphabetic(char ch)
    {
        if (ch > 255)
        {
            return false;
        }

        return (Flags[ch] & IsAlphaFlag) != 0;
    }

    private bool IsHexadecimalDigit(char ch)
    {
        if (ch > 255)
        {
            return false;
        }

        return (Flags[ch] & IsHexDigitFlag) != 0;
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
