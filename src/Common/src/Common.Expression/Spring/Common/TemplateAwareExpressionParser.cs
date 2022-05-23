// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Common
{
    public abstract class TemplateAwareExpressionParser : IExpressionParser
    {
        public IExpression ParseExpression(string expressionString)
        {
            return ParseExpression(expressionString, null);
        }

        public IExpression ParseExpression(string expressionString, IParserContext context)
        {
            if (context != null && context.IsTemplate)
            {
                return ParseTemplate(expressionString, context);
            }
            else
            {
                return DoParseExpression(expressionString, context);
            }
        }

        protected internal abstract IExpression DoParseExpression(string expressionString, IParserContext context);

        private IExpression ParseTemplate(string expressionString, IParserContext context)
        {
            if (expressionString.Length == 0)
            {
                return new LiteralExpression(string.Empty);
            }

            var expressions = ParseExpressions(expressionString, context);
            if (expressions.Count == 1)
            {
                return expressions[0];
            }
            else
            {
                return new CompositeStringExpression(expressionString, expressions);
            }
        }

        private List<IExpression> ParseExpressions(string expressionString, IParserContext context)
        {
            var expressions = new List<IExpression>();
            var prefix = context.ExpressionPrefix;
            var suffix = context.ExpressionSuffix;
            var startIdx = 0;

            while (startIdx < expressionString.Length)
            {
                var prefixIndex = expressionString.IndexOf(prefix, startIdx);
                if (prefixIndex >= startIdx)
                {
                    // an inner expression was found - this is a composite
                    if (prefixIndex > startIdx)
                    {
                        expressions.Add(new LiteralExpression(expressionString.Substring(startIdx, prefixIndex - startIdx)));
                    }

                    var afterPrefixIndex = prefixIndex + prefix.Length;
                    var suffixIndex = SkipToCorrectEndSuffix(suffix, expressionString, afterPrefixIndex);
                    if (suffixIndex == -1)
                    {
                        throw new ParseException(expressionString, prefixIndex, $"No ending suffix '{suffix}' for expression starting at character {prefixIndex}: {expressionString.Substring(prefixIndex)}");
                    }

                    if (suffixIndex == afterPrefixIndex)
                    {
                        throw new ParseException(expressionString, prefixIndex, $"No expression defined within delimiter '{prefix}{suffix}' at character {prefixIndex}");
                    }

                    var startIndex = prefixIndex + prefix.Length;
                    var expr = expressionString.Substring(startIndex, suffixIndex - startIndex);
                    expr = expr.Trim();
                    if (expr.Length == 0)
                    {
                        throw new ParseException(expressionString, prefixIndex, $"No expression defined within delimiter '{prefix}{suffix}' at character {prefixIndex}");
                    }

                    expressions.Add(DoParseExpression(expr, context));
                    startIdx = suffixIndex + suffix.Length;
                }
                else
                {
                    // no more ${expressions} found in string, add rest as static text
                    expressions.Add(new LiteralExpression(expressionString.Substring(startIdx)));
                    startIdx = expressionString.Length;
                }
            }

            return expressions;
        }

        private bool IsSuffixHere(string expressionString, int pos, string suffix)
        {
            var suffixPosition = 0;
            for (var i = 0; i < suffix.Length && pos < expressionString.Length; i++)
            {
                if (expressionString[pos++] != suffix[suffixPosition++])
                {
                    return false;
                }
            }

            if (suffixPosition != suffix.Length)
            {
                // the expressionString ran out before the suffix could entirely be found
                return false;
            }

            return true;
        }

        private int SkipToCorrectEndSuffix(string suffix, string expressionString, int afterPrefixIndex)
        {
            // Chew on the expression text - relying on the rules:
            // brackets must be in pairs: () [] {}
            // string literals are "..." or '...' and these may contain unmatched brackets
            var pos = afterPrefixIndex;
            var maxlen = expressionString.Length;
            var nextSuffix = expressionString.IndexOf(suffix, afterPrefixIndex);
            if (nextSuffix == -1)
            {
                return -1; // the suffix is missing
            }

            var stack = new Stack<Bracket>();
            while (pos < maxlen)
            {
                if (IsSuffixHere(expressionString, pos, suffix) && stack.Count == 0)
                {
                    break;
                }

                var ch = expressionString[pos];
                switch (ch)
                {
                    case '{':
                    case '[':
                    case '(':
                        stack.Push(new Bracket(ch, pos));
                        break;
                    case '}':
                    case ']':
                    case ')':
                        if (stack.Count == 0)
                        {
                            throw new ParseException(expressionString, pos, $"Found closing '{ch}' at position {pos} without an opening '{Bracket.TheOpenBracketFor(ch)}'");
                        }

                        var p = stack.Pop();
                        if (!p.CompatibleWithCloseBracket(ch))
                        {
                            throw new ParseException(expressionString, pos, $"Found closing '{ch}' at position {pos} but most recent opening is '{p.BracketChar}' at position {p.Pos}");
                        }

                        break;
                    case '\'':
                    case '"':
                        // jump to the end of the literal
                        var endLiteral = expressionString.IndexOf(ch, pos + 1);
                        if (endLiteral == -1)
                        {
                            throw new ParseException(expressionString, pos, $"Found non terminating string literal starting at position {pos}");
                        }

                        pos = endLiteral;
                        break;
                }

                pos++;
            }

            if (stack.Count > 0)
            {
                var p = stack.Pop();
                throw new ParseException(expressionString, p.Pos, $"Missing closing '{Bracket.TheCloseBracketFor(p.BracketChar)}' for '{p.BracketChar}' at position {p.Pos}");
            }

            if (!IsSuffixHere(expressionString, pos, suffix))
            {
                return -1;
            }

            return pos;
        }

        private class Bracket
        {
            public char BracketChar { get; }

            public int Pos { get; }

            public Bracket(char bracket, int pos)
            {
                BracketChar = bracket;
                Pos = pos;
            }

            public static char TheOpenBracketFor(char closeBracket)
            {
                if (closeBracket == '}')
                {
                    return '{';
                }
                else if (closeBracket == ']')
                {
                    return '[';
                }

                return '(';
            }

            public static char TheCloseBracketFor(char openBracket)
            {
                if (openBracket == '{')
                {
                    return '}';
                }
                else if (openBracket == '[')
                {
                    return ']';
                }

                return ')';
            }

            public bool CompatibleWithCloseBracket(char closeBracket)
            {
                if (BracketChar == '{')
                {
                    return closeBracket == '}';
                }
                else if (BracketChar == '[')
                {
                    return closeBracket == ']';
                }

                return closeBracket == ')';
            }
        }
    }
}
