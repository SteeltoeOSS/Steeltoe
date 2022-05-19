// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public abstract class Literal : SpelNode
    {
        protected Literal(string originalValue, int startPos, int endPos)
            : base(startPos, endPos)
        {
            OriginalValue = originalValue;
        }

        public string OriginalValue { get; }

        public static Literal GetIntLiteral(string numberToken, int startPos, int endPos, NumberStyles radix)
        {
            try
            {
                var value = int.Parse(numberToken, radix);
                if (radix == NumberStyles.HexNumber && value < 0)
                {
                    throw new InternalParseException(new SpelParseException(startPos, new FormatException("Hex parse error"), SpelMessage.NOT_AN_INTEGER, numberToken));
                }

                return new IntLiteral(numberToken, startPos, endPos, value);
            }
            catch (FormatException ex)
            {
                throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NOT_AN_INTEGER, numberToken));
            }
        }

        public static Literal GetLongLiteral(string numberToken, int startPos, int endPos, NumberStyles radix)
        {
            try
            {
                var value = long.Parse(numberToken, radix);
                if (radix == NumberStyles.HexNumber && value < 0)
                {
                    throw new InternalParseException(new SpelParseException(startPos, new FormatException("Hex parse error"), SpelMessage.NOT_A_LONG, numberToken));
                }

                return new LongLiteral(numberToken, startPos, endPos, value);
            }
            catch (FormatException ex)
            {
                throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NOT_A_LONG, numberToken));
            }
        }

        public static Literal GetRealLiteral(string numberToken, int startPos, int endPos, bool isFloat)
        {
            try
            {
                var toParse = GetNumberLiteral(numberToken);
                if (isFloat)
                {
                    var value = float.Parse(toParse);
                    return new FloatLiteral(numberToken, startPos, endPos, value);
                }
                else
                {
                    var value = double.Parse(toParse);
                    return new RealLiteral(numberToken, startPos, endPos, value);
                }
            }
            catch (FormatException ex)
            {
                throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NOT_A_REAL, numberToken));
            }
        }

        public static string GetNumberLiteral(string numberToken)
        {
            if (numberToken[numberToken.Length - 1] == 'd' ||
                numberToken[numberToken.Length - 1] == 'D' ||
                numberToken[numberToken.Length - 1] == 'f' ||
                numberToken[numberToken.Length - 1] == 'F')
            {
                return numberToken.Substring(0, numberToken.Length - 1);
            }

            return numberToken;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            return GetLiteralValue();
        }

        public override string ToString()
        {
            return GetLiteralValue().Value.ToString();
        }

        public override string ToStringAST()
        {
            return ToString();
        }

        public abstract ITypedValue GetLiteralValue();
    }
}
