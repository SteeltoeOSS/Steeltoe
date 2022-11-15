// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public abstract class Literal : SpelNode
{
    public string OriginalValue { get; }

    protected Literal(string originalValue, int startPos, int endPos)
        : base(startPos, endPos)
    {
        OriginalValue = originalValue;
    }

    public static Literal GetIntLiteral(string numberToken, int startPos, int endPos, NumberStyles radix)
    {
        try
        {
            int value = int.Parse(numberToken, radix, CultureInfo.InvariantCulture);

            if (radix == NumberStyles.HexNumber && value < 0)
            {
                throw new InternalParseException(
                    new SpelParseException(startPos, new FormatException("Hex parse error"), SpelMessage.NotAnInteger, numberToken));
            }

            return new IntLiteral(numberToken, startPos, endPos, value);
        }
        catch (FormatException ex)
        {
            throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NotAnInteger, numberToken));
        }
    }

    public static Literal GetLongLiteral(string numberToken, int startPos, int endPos, NumberStyles radix)
    {
        try
        {
            long value = long.Parse(numberToken, radix, CultureInfo.InvariantCulture);

            if (radix == NumberStyles.HexNumber && value < 0)
            {
                throw new InternalParseException(new SpelParseException(startPos, new FormatException("Hex parse error"), SpelMessage.NotALong, numberToken));
            }

            return new LongLiteral(numberToken, startPos, endPos, value);
        }
        catch (FormatException ex)
        {
            throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NotALong, numberToken));
        }
    }

    public static Literal GetRealLiteral(string numberToken, int startPos, int endPos, bool isFloat)
    {
        try
        {
            string toParse = GetNumberLiteral(numberToken);

            if (isFloat)
            {
                float value = float.Parse(toParse, CultureInfo.InvariantCulture);
                return new FloatLiteral(numberToken, startPos, endPos, value);
            }
            else
            {
                double value = double.Parse(toParse, CultureInfo.InvariantCulture);
                return new RealLiteral(numberToken, startPos, endPos, value);
            }
        }
        catch (FormatException ex)
        {
            throw new InternalParseException(new SpelParseException(startPos, ex, SpelMessage.NotAReal, numberToken));
        }
    }

    public static string GetNumberLiteral(string numberToken)
    {
        if (numberToken[numberToken.Length - 1] == 'd' || numberToken[numberToken.Length - 1] == 'D' || numberToken[numberToken.Length - 1] == 'f' ||
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
        object value = GetLiteralValue().Value;

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        return value.ToString();
    }

    public override string ToStringAst()
    {
        return ToString();
    }

    public abstract ITypedValue GetLiteralValue();
}
