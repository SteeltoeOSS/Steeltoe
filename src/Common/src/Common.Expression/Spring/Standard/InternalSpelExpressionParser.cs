// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard
{
    public class InternalSpelExpressionParser : TemplateAwareExpressionParser
    {
        public InternalSpelExpressionParser(SpelParserOptions configuration)
        {
            Configuration = configuration;
        }

        private static readonly Regex VALID_QUALIFIED_ID_PATTERN = new("[\\p{L}\\p{N}_$]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        internal SpelParserOptions Configuration { get; }

        internal Stack<SpelNode> ConstructedNodes = new ();

        internal string ExpressionString { get; private set; }

        internal List<Token> TokenStream { get; private set; } = new List<Token>();

        internal int TokenStreamLength { get; private set; }

        internal int TokenStreamPointer { get; private set; }

        protected internal override IExpression DoParseExpression(string expressionString, IParserContext context)
        {
            try
            {
                ExpressionString = expressionString;
                var tokenizer = new Tokenizer(expressionString);
                TokenStream = tokenizer.Process();
                TokenStreamLength = TokenStream.Count;
                TokenStreamPointer = 0;
                ConstructedNodes.Clear();
                var ast = EatExpression();
                if (ast == null)
                {
                    throw new InvalidOperationException("No node");
                }

                var t = PeekToken();
                if (t != null)
                {
                    throw new SpelParseException(t.StartPos, SpelMessage.MORE_INPUT, ToString(NextToken()));
                }

                if (ConstructedNodes.Count != 0)
                {
                    throw new InvalidOperationException("At least one node expected");
                }

                return new SpelExpression(expressionString, ast, Configuration);
            }
            catch (InternalParseException ex)
            {
                throw ex.InnerException;
            }
        }

        private SpelNode EatExpression()
        {
            var expr = EatLogicalOrExpression();
            var t = PeekToken();
            if (t != null)
            {
                if (t.Kind == TokenKind.ASSIGN)
                {
                    // a=b
                    if (expr == null)
                    {
                        expr = new NullLiteral(t.StartPos - 1, t.EndPos - 1);
                    }

                    NextToken();
                    var assignedValue = EatLogicalOrExpression();
                    return new Assign(t.StartPos, t.EndPos, expr, assignedValue);
                }

                if (t.Kind == TokenKind.ELVIS)
                {
                    // a?:b (a if it isn't null, otherwise b)
                    if (expr == null)
                    {
                        expr = new NullLiteral(t.StartPos - 1, t.EndPos - 2);
                    }

                    NextToken();  // elvis has left the building
                    var valueIfNull = EatExpression();
                    if (valueIfNull == null)
                    {
                        valueIfNull = new NullLiteral(t.StartPos + 1, t.EndPos + 1);
                    }

                    return new Elvis(t.StartPos, t.EndPos, expr, valueIfNull);
                }

                if (t.Kind == TokenKind.QMARK)
                {
                    // a?b:c
                    if (expr == null)
                    {
                        expr = new NullLiteral(t.StartPos - 1, t.EndPos - 1);
                    }

                    NextToken();
                    var ifTrueExprValue = EatExpression();
                    EatToken(TokenKind.COLON);
                    var ifFalseExprValue = EatExpression();
                    return new Ternary(t.StartPos, t.EndPos, expr, ifTrueExprValue, ifFalseExprValue);
                }
            }

            return expr;
        }

        private SpelNode EatLogicalOrExpression()
        {
            var expr = EatLogicalAndExpression();
            while (PeekIdentifierToken("or") || PeekToken(TokenKind.SYMBOLIC_OR))
            {
                // consume OR
                var t = TakeToken();
                var rhExpr = EatLogicalAndExpression();
                CheckOperands(t, expr, rhExpr);
                expr = new OpOr(t.StartPos, t.EndPos, expr, rhExpr);
            }

            return expr;
        }

        private SpelNode EatLogicalAndExpression()
        {
            var expr = EatRelationalExpression();
            while (PeekIdentifierToken("and") || PeekToken(TokenKind.SYMBOLIC_AND))
            {
                // consume 'AND'
                var t = TakeToken();
                var rhExpr = EatRelationalExpression();
                CheckOperands(t, expr, rhExpr);
                expr = new OpAnd(t.StartPos, t.EndPos, expr, rhExpr);
            }

            return expr;
        }

        private SpelNode EatRelationalExpression()
        {
            var expr = EatSumExpression();
            var relationalOperatorToken = MaybeEatRelationalOperator();
            if (relationalOperatorToken != null)
            {
                // consume relational operator token
                var t = TakeToken();
                var rhExpr = EatSumExpression();
                CheckOperands(t, expr, rhExpr);
                var tk = relationalOperatorToken.Kind;

                if (relationalOperatorToken.IsNumericRelationalOperator)
                {
                    if (tk == TokenKind.GT)
                    {
                        return new OpGT(t.StartPos, t.EndPos, expr, rhExpr);
                    }

                    if (tk == TokenKind.LT)
                    {
                        return new OpLT(t.StartPos, t.EndPos, expr, rhExpr);
                    }

                    if (tk == TokenKind.LE)
                    {
                        return new OpLE(t.StartPos, t.EndPos, expr, rhExpr);
                    }

                    if (tk == TokenKind.GE)
                    {
                        return new OpGE(t.StartPos, t.EndPos, expr, rhExpr);
                    }

                    if (tk == TokenKind.EQ)
                    {
                        return new OpEQ(t.StartPos, t.EndPos, expr, rhExpr);
                    }

                    if (tk != TokenKind.NE)
                    {
                        throw new InvalidOperationException("Not-equals token expected");
                    }

                    return new OpNE(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (tk == TokenKind.INSTANCEOF)
                {
                    return new OperatorInstanceof(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (tk == TokenKind.MATCHES)
                {
                    return new OperatorMatches(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (tk != TokenKind.BETWEEN)
                {
                    throw new InvalidOperationException("Between token expected");
                }

                return new OperatorBetween(t.StartPos, t.EndPos, expr, rhExpr);
            }

            return expr;
        }

        private SpelNode EatSumExpression()
        {
            var expr = EatProductExpression();
            while (PeekToken(TokenKind.PLUS, TokenKind.MINUS, TokenKind.INC))
            {
                // consume PLUS or MINUS or INC
                var t = TakeToken();
                var rhExpr = EatProductExpression();
                CheckRightOperand(t, rhExpr);
                if (t.Kind == TokenKind.PLUS)
                {
                    expr = new OpPlus(t.StartPos, t.EndPos, expr, rhExpr);
                }
                else if (t.Kind == TokenKind.MINUS)
                {
                    expr = new OpMinus(t.StartPos, t.EndPos, expr, rhExpr);
                }
            }

            return expr;
        }

        private SpelNode EatProductExpression()
        {
            var expr = EatPowerIncDecExpression();
            while (PeekToken(TokenKind.STAR, TokenKind.DIV, TokenKind.MOD))
            {
                var t = TakeToken();  // consume STAR/DIV/MOD
                var rhExpr = EatPowerIncDecExpression();
                CheckOperands(t, expr, rhExpr);
                if (t.Kind == TokenKind.STAR)
                {
                    expr = new OpMultiply(t.StartPos, t.EndPos, expr, rhExpr);
                }
                else if (t.Kind == TokenKind.DIV)
                {
                    expr = new OpDivide(t.StartPos, t.EndPos, expr, rhExpr);
                }
                else
                {
                    if (t.Kind != TokenKind.MOD)
                    {
                        throw new InvalidOperationException("Mod token expected");
                    }

                    expr = new OpModulus(t.StartPos, t.EndPos, expr, rhExpr);
                }
            }

            return expr;
        }

        private SpelNode EatPowerIncDecExpression()
        {
            var expr = EatUnaryExpression();
            if (PeekToken(TokenKind.POWER))
            {
                var t = TakeToken();  // consume POWER
                var rhExpr = EatUnaryExpression();
                CheckRightOperand(t, rhExpr);
                return new OperatorPower(t.StartPos, t.EndPos, expr, rhExpr);
            }

            if (expr != null && PeekToken(TokenKind.INC, TokenKind.DEC))
            {
                var t = TakeToken();  // consume INC/DEC
                if (t.Kind == TokenKind.INC)
                {
                    return new OpInc(t.StartPos, t.EndPos, true, expr);
                }

                return new OpDec(t.StartPos, t.EndPos, true, expr);
            }

            return expr;
        }

        private SpelNode EatUnaryExpression()
        {
            if (PeekToken(TokenKind.PLUS, TokenKind.MINUS, TokenKind.NOT))
            {
                var t = TakeToken();
                var expr = EatUnaryExpression();
                if (expr == null)
                {
                    throw new InvalidOperationException("No node");
                }

                if (t.Kind == TokenKind.NOT)
                {
                    return new OperatorNot(t.StartPos, t.EndPos, expr);
                }

                if (t.Kind == TokenKind.PLUS)
                {
                    return new OpPlus(t.StartPos, t.EndPos, expr);
                }

                if (t.Kind != TokenKind.MINUS)
                {
                    throw new InvalidOperationException("Minus token expected");
                }

                return new OpMinus(t.StartPos, t.EndPos, expr);
            }

            if (PeekToken(TokenKind.INC, TokenKind.DEC))
            {
                var t = TakeToken();
                var expr = EatUnaryExpression();
                if (t.Kind == TokenKind.INC)
                {
                    return new OpInc(t.StartPos, t.EndPos, false, expr);
                }

                return new OpDec(t.StartPos, t.EndPos, false, expr);
            }

            return EatPrimaryExpression();
        }

        private SpelNode EatPrimaryExpression()
        {
            var start = EatStartNode();  // always a start node
            List<SpelNode> nodes = null;
            var node = EatNode();
            while (node != null)
            {
                if (nodes == null)
                {
                    nodes = new List<SpelNode>(4)
                    {
                        start
                    };
                }

                nodes.Add(node);
                node = EatNode();
            }

            if (start == null || nodes == null)
            {
                return start;
            }

            return new CompoundExpression(start.StartPosition, nodes[nodes.Count - 1].EndPosition, nodes.ToArray());
        }

        private SpelNode EatNode()
        {
            return PeekToken(TokenKind.DOT, TokenKind.SAFE_NAVI) ? EatDottedNode() : EatNonDottedNode();
        }

        private SpelNode EatNonDottedNode()
        {
            if (PeekToken(TokenKind.LSQUARE) && MaybeEatIndexer())
            {
                return Pop();
            }

            return null;
        }

        private SpelNode EatDottedNode()
        {
            var t = TakeToken();  // it was a '.' or a '?.'
            var nullSafeNavigation = t.Kind == TokenKind.SAFE_NAVI;
            if (MaybeEatMethodOrProperty(nullSafeNavigation) || MaybeEatFunctionOrVar() ||
                    MaybeEatProjection(nullSafeNavigation) || MaybeEatSelection(nullSafeNavigation))
            {
                return Pop();
            }

            if (PeekToken() == null)
            {
                // unexpectedly ran out of data
                throw InternalException(t.StartPos, SpelMessage.OOD);
            }
            else
            {
                throw InternalException(t.StartPos, SpelMessage.UNEXPECTED_DATA_AFTER_DOT, ToString(PeekToken()));
            }
        }

        private bool MaybeEatFunctionOrVar()
        {
            if (!PeekToken(TokenKind.HASH))
            {
                return false;
            }

            var t = TakeToken();
            var functionOrVariableName = EatToken(TokenKind.IDENTIFIER);
            var args = MaybeEatMethodArgs();
            if (args == null)
            {
                Push(new VariableReference(functionOrVariableName.StringValue, t.StartPos, functionOrVariableName.EndPos));
                return true;
            }

            Push(new FunctionReference(functionOrVariableName.StringValue, t.StartPos, functionOrVariableName.EndPos, args));
            return true;
        }

        private SpelNode[] MaybeEatMethodArgs()
        {
            if (!PeekToken(TokenKind.LPAREN))
            {
                return null;
            }

            var args = new List<SpelNode>();
            ConsumeArguments(args);
            EatToken(TokenKind.RPAREN);
            return args.ToArray();
        }

        private void EatConstructorArgs(List<SpelNode> accumulatedArguments)
        {
            if (!PeekToken(TokenKind.LPAREN))
            {
                throw new InternalParseException(new SpelParseException(ExpressionString, PositionOf(PeekToken()), SpelMessage.MISSING_CONSTRUCTOR_ARGS));
            }

            ConsumeArguments(accumulatedArguments);
            EatToken(TokenKind.RPAREN);
        }

        private void ConsumeArguments(List<SpelNode> accumulatedArguments)
        {
            var t = PeekToken();
            if (t == null)
            {
                throw new InvalidOperationException("Expected token");
            }

            var pos = t.StartPos;
            Token next;
            do
            {
                NextToken();  // consume (first time through) or comma (subsequent times)
                t = PeekToken();
                if (t == null)
                {
                    throw InternalException(pos, SpelMessage.RUN_OUT_OF_ARGUMENTS);
                }

                if (t.Kind != TokenKind.RPAREN)
                {
                    accumulatedArguments.Add(EatExpression());
                }

                next = PeekToken();
            }
            while (next != null && next.Kind == TokenKind.COMMA);

            if (next == null)
            {
                throw InternalException(pos, SpelMessage.RUN_OUT_OF_ARGUMENTS);
            }
        }

        private int PositionOf(Token t)
        {
            if (t == null)
            {
                // if null assume the problem is because the right token was
                // not found at the end of the expression
                return ExpressionString.Length;
            }

            return t.StartPos;
        }

        private SpelNode EatStartNode()
        {
            if (MaybeEatLiteral())
            {
                return Pop();
            }
            else if (MaybeEatParenExpression())
            {
                return Pop();
            }
            else if (MaybeEatTypeReference() || MaybeEatNullReference() || MaybeEatConstructorReference() ||
                    MaybeEatMethodOrProperty(false) || MaybeEatFunctionOrVar())
            {
                return Pop();
            }
            else if (MaybeEatServiceReference())
            {
                return Pop();
            }
            else if (MaybeEatProjection(false) || MaybeEatSelection(false) || MaybeEatIndexer())
            {
                return Pop();
            }
            else if (MaybeEatInlineListOrMap())
            {
                return Pop();
            }
            else
            {
                return null;
            }
        }

        private bool MaybeEatServiceReference()
        {
            if (PeekToken(TokenKind.SERVICE_REF) || PeekToken(TokenKind.FACTORY_SERVICE_REF))
            {
                var serviceRefToken = TakeToken();
                Token serviceNameToken = null;
                string serviceName = null;
                if (PeekToken(TokenKind.IDENTIFIER))
                {
                    serviceNameToken = EatToken(TokenKind.IDENTIFIER);
                    serviceName = serviceNameToken.StringValue;
                }
                else if (PeekToken(TokenKind.LITERAL_STRING))
                {
                    serviceNameToken = EatToken(TokenKind.LITERAL_STRING);
                    serviceName = serviceNameToken.StringValue;
                    serviceName = serviceName.Substring(1, serviceName.Length - 1 - 1);
                }
                else
                {
                    throw InternalException(serviceRefToken.StartPos, SpelMessage.INVALID_SERVICE_REFERENCE);
                }

                ServiceReference serviceReference;
                if (serviceRefToken.Kind == TokenKind.FACTORY_SERVICE_REF)
                {
                    var serviceNameString = new string(TokenKind.FACTORY_SERVICE_REF.TokenChars) + serviceName;
                    serviceReference = new ServiceReference(serviceRefToken.StartPos, serviceNameToken.EndPos, serviceNameString);
                }
                else
                {
                    serviceReference = new ServiceReference(serviceNameToken.StartPos, serviceNameToken.EndPos, serviceName);
                }

                ConstructedNodes.Push(serviceReference);
                return true;
            }

            return false;
        }

        private bool MaybeEatTypeReference()
        {
            if (PeekToken(TokenKind.IDENTIFIER))
            {
                var typeName = PeekToken();
                if (typeName == null)
                {
                    throw new InvalidOperationException("Expected token");
                }

                if (!"T".Equals(typeName.StringValue))
                {
                    return false;
                }

                // It looks like a type reference but is T being used as a map key?
                var t = TakeToken();
                if (PeekToken(TokenKind.RSQUARE))
                {
                    // looks like 'T]' (T is map key)
                    Push(new PropertyOrFieldReference(false, t.StringValue, t.StartPos, t.EndPos));
                    return true;
                }

                EatToken(TokenKind.LPAREN);
                var node = EatPossiblyQualifiedId();

                // dotted qualified id
                // Are there array dimensions?
                var dims = 0;
                while (PeekToken(TokenKind.LSQUARE, true))
                {
                    EatToken(TokenKind.RSQUARE);
                    dims++;
                }

                EatToken(TokenKind.RPAREN);
                ConstructedNodes.Push(new TypeReference(typeName.StartPos, typeName.EndPos, node, dims));
                return true;
            }

            return false;
        }

        private bool MaybeEatNullReference()
        {
            if (PeekToken(TokenKind.IDENTIFIER))
            {
                var nullToken = PeekToken();
                if (nullToken == null)
                {
                    throw new InvalidOperationException("Expected token");
                }

                if (!"null".Equals(nullToken.StringValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }

                NextToken();
                ConstructedNodes.Push(new NullLiteral(nullToken.StartPos, nullToken.EndPos));
                return true;
            }

            return false;
        }

        private bool MaybeEatProjection(bool nullSafeNavigation)
        {
            var t = PeekToken();
            if (!PeekToken(TokenKind.PROJECT, true))
            {
                return false;
            }

            if (t == null)
            {
                throw new InvalidOperationException("No token");
            }

            var expr = EatExpression();
            if (expr == null)
            {
                throw new InvalidOperationException("No node");
            }

            EatToken(TokenKind.RSQUARE);
            ConstructedNodes.Push(new Projection(nullSafeNavigation, t.StartPos, t.EndPos, expr));
            return true;
        }

        private bool MaybeEatInlineListOrMap()
        {
            var t = PeekToken();
            if (!PeekToken(TokenKind.LCURLY, true))
            {
                return false;
            }

            if (t == null)
            {
                throw new InvalidOperationException("No token");
            }

            SpelNode expr = null;
            var closingCurly = PeekToken();
            if (PeekToken(TokenKind.RCURLY, true))
            {
                // empty list '{}'
                if (closingCurly == null)
                {
                    throw new InvalidOperationException("No token");
                }

                expr = new InlineList(t.StartPos, closingCurly.EndPos);
            }
            else if (PeekToken(TokenKind.COLON, true))
            {
                closingCurly = EatToken(TokenKind.RCURLY);

                // empty map '{:}'
                expr = new InlineMap(t.StartPos, closingCurly.EndPos);
            }
            else
            {
                var firstExpression = EatExpression();

                // Next is either:
                // '}' - end of list
                // ',' - more expressions in this list
                // ':' - this is a map!
                if (PeekToken(TokenKind.RCURLY))
                {
                    // list with one item in it
                    var elements = new List<SpelNode>
                    {
                        firstExpression
                    };
                    closingCurly = EatToken(TokenKind.RCURLY);
                    expr = new InlineList(t.StartPos, closingCurly.EndPos, elements.ToArray());
                }
                else if (PeekToken(TokenKind.COMMA, true))
                {
                    // multi-item list
                    var elements = new List<SpelNode>
                    {
                        firstExpression
                    };
                    do
                    {
                        elements.Add(EatExpression());
                    }
                    while (PeekToken(TokenKind.COMMA, true));
                    closingCurly = EatToken(TokenKind.RCURLY);
                    expr = new InlineList(t.StartPos, closingCurly.EndPos, elements.ToArray());
                }
                else if (PeekToken(TokenKind.COLON, true))
                {
                    // map!
                    var elements = new List<SpelNode>
                    {
                        firstExpression,
                        EatExpression()
                    };
                    while (PeekToken(TokenKind.COMMA, true))
                    {
                        elements.Add(EatExpression());
                        EatToken(TokenKind.COLON);
                        elements.Add(EatExpression());
                    }

                    closingCurly = EatToken(TokenKind.RCURLY);
                    expr = new InlineMap(t.StartPos, closingCurly.EndPos, elements.ToArray());
                }
                else
                {
                    throw InternalException(t.StartPos, SpelMessage.OOD);
                }
            }

            ConstructedNodes.Push(expr);
            return true;
        }

        private bool MaybeEatIndexer()
        {
            var t = PeekToken();
            if (!PeekToken(TokenKind.LSQUARE, true))
            {
                return false;
            }

            if (t == null)
            {
                throw new InvalidOperationException("No token");
            }

            var expr = EatExpression();
            if (expr == null)
            {
                throw new InvalidOperationException("No node");
            }

            EatToken(TokenKind.RSQUARE);
            ConstructedNodes.Push(new Indexer(t.StartPos, t.EndPos, expr));
            return true;
        }

        private bool MaybeEatSelection(bool nullSafeNavigation)
        {
            var t = PeekToken();
            if (!PeekSelectToken())
            {
                return false;
            }

            if (t == null)
            {
                throw new InvalidOperationException("No token");
            }

            NextToken();
            var expr = EatExpression();
            if (expr == null)
            {
                throw InternalException(t.StartPos, SpelMessage.MISSING_SELECTION_EXPRESSION);
            }

            EatToken(TokenKind.RSQUARE);
            if (t.Kind == TokenKind.SELECT_FIRST)
            {
                ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.FIRST, t.StartPos, t.EndPos, expr));
            }
            else if (t.Kind == TokenKind.SELECT_LAST)
            {
                ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.LAST, t.StartPos, t.EndPos, expr));
            }
            else
            {
                ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.ALL, t.StartPos, t.EndPos, expr));
            }

            return true;
        }

        private SpelNode EatPossiblyQualifiedId()
        {
            var qualifiedIdPieces = new List<SpelNode>();
            var node = PeekToken();
            while (IsValidQualifiedId(node))
            {
                NextToken();
                if (node.Kind != TokenKind.DOT)
                {
                    qualifiedIdPieces.Add(new Identifier(node.StringValue, node.StartPos, node.EndPos));
                }

                node = PeekToken();
            }

            if (qualifiedIdPieces.Count == 0)
            {
                if (node == null)
                {
                    throw InternalException(ExpressionString.Length, SpelMessage.OOD);
                }

                throw InternalException(node.StartPos, SpelMessage.NOT_EXPECTED_TOKEN, "qualified ID", node.Kind.ToString().ToLower());
            }

            return new QualifiedIdentifier(qualifiedIdPieces.First().StartPosition, qualifiedIdPieces.Last().EndPosition, qualifiedIdPieces.ToArray());
        }

        private bool IsValidQualifiedId(Token node)
        {
            if (node == null || node.Kind == TokenKind.LITERAL_STRING)
            {
                return false;
            }

            if (node.Kind == TokenKind.DOT || node.Kind == TokenKind.IDENTIFIER)
            {
                return true;
            }

            var value = node.StringValue;
            return !string.IsNullOrEmpty(value) && VALID_QUALIFIED_ID_PATTERN.Matches(value).Count > 0;
        }

        private bool MaybeEatMethodOrProperty(bool nullSafeNavigation)
        {
            if (PeekToken(TokenKind.IDENTIFIER))
            {
                var methodOrPropertyName = TakeToken();
                var args = MaybeEatMethodArgs();
                if (args == null)
                {
                    // property
                    Push(new PropertyOrFieldReference(nullSafeNavigation, methodOrPropertyName.StringValue, methodOrPropertyName.StartPos, methodOrPropertyName.EndPos));
                    return true;
                }

                // method reference
                Push(new MethodReference(nullSafeNavigation, methodOrPropertyName.StringValue, methodOrPropertyName.StartPos, methodOrPropertyName.EndPos, args));

                // TODO what is the end position for a method reference? the name or the last arg?
                return true;
            }

            return false;
        }

        private bool MaybeEatConstructorReference()
        {
            if (PeekIdentifierToken("new"))
            {
                var newToken = TakeToken();

                // It looks like a constructor reference but is NEW being used as a map key?
                if (PeekToken(TokenKind.RSQUARE))
                {
                    // looks like 'NEW]' (so NEW used as map key)
                    Push(new PropertyOrFieldReference(false, newToken.StringValue, newToken.StartPos, newToken.EndPos));
                    return true;
                }

                var possiblyQualifiedConstructorName = EatPossiblyQualifiedId();
                var nodes = new List<SpelNode>
                {
                    possiblyQualifiedConstructorName
                };
                if (PeekToken(TokenKind.LSQUARE))
                {
                    // array initializer
                    var dimensions = new List<SpelNode>();
                    while (PeekToken(TokenKind.LSQUARE, true))
                    {
                        if (!PeekToken(TokenKind.RSQUARE))
                        {
                            dimensions.Add(EatExpression());
                        }
                        else
                        {
                            dimensions.Add(null);
                        }

                        EatToken(TokenKind.RSQUARE);
                    }

                    if (MaybeEatInlineListOrMap())
                    {
                        nodes.Add(Pop());
                    }

                    Push(new ConstructorReference(newToken.StartPos, newToken.EndPos, dimensions.ToArray(), nodes.ToArray()));
                }
                else
                {
                    // regular constructor invocation
                    EatConstructorArgs(nodes);

                    // TODO correct end position?
                    Push(new ConstructorReference(newToken.StartPos, newToken.EndPos, nodes.ToArray()));
                }

                return true;
            }

            return false;
        }

        private void Push(SpelNode newNode)
        {
            ConstructedNodes.Push(newNode);
        }

        private SpelNode Pop()
        {
            return ConstructedNodes.Pop();
        }

        private bool MaybeEatLiteral()
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            if (t.Kind == TokenKind.LITERAL_INT)
            {
                Push(Literal.GetIntLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.Integer));
            }
            else if (t.Kind == TokenKind.LITERAL_LONG)
            {
                Push(Literal.GetLongLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.Integer));
            }
            else if (t.Kind == TokenKind.LITERAL_HEXINT)
            {
                Push(Literal.GetIntLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.HexNumber));
            }
            else if (t.Kind == TokenKind.LITERAL_HEXLONG)
            {
                Push(Literal.GetLongLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.HexNumber));
            }
            else if (t.Kind == TokenKind.LITERAL_REAL)
            {
                Push(Literal.GetRealLiteral(t.StringValue, t.StartPos, t.EndPos, false));
            }
            else if (t.Kind == TokenKind.LITERAL_REAL_FLOAT)
            {
                Push(Literal.GetRealLiteral(t.StringValue, t.StartPos, t.EndPos, true));
            }
            else if (PeekIdentifierToken("true"))
            {
                Push(new BooleanLiteral(t.StringValue, t.StartPos, t.EndPos, true));
            }
            else if (PeekIdentifierToken("false"))
            {
                Push(new BooleanLiteral(t.StringValue, t.StartPos, t.EndPos, false));
            }
            else if (t.Kind == TokenKind.LITERAL_STRING)
            {
                Push(new StringLiteral(t.StringValue, t.StartPos, t.EndPos, t.StringValue));
            }
            else
            {
                return false;
            }

            NextToken();
            return true;
        }

        private bool MaybeEatParenExpression()
        {
            if (PeekToken(TokenKind.LPAREN))
            {
                NextToken();
                var expr = EatExpression();
                if (expr == null)
                {
                    throw new InvalidOperationException("No node");
                }

                EatToken(TokenKind.RPAREN);
                Push(expr);
                return true;
            }
            else
            {
                return false;
            }
        }

        private Token MaybeEatRelationalOperator()
        {
            var t = PeekToken();
            if (t == null)
            {
                return null;
            }

            if (t.IsNumericRelationalOperator)
            {
                return t;
            }

            if (t.IsIdentifier)
            {
                var idString = t.StringValue;
                if (idString.Equals("instanceof", StringComparison.InvariantCultureIgnoreCase))
                {
                    return t.AsInstanceOfToken();
                }

                if (idString.Equals("matches", StringComparison.InvariantCultureIgnoreCase))
                {
                    return t.AsMatchesToken();
                }

                if (idString.Equals("between", StringComparison.InvariantCultureIgnoreCase))
                {
                    return t.AsBetweenToken();
                }
            }

            return null;
        }

        private Token EatToken(TokenKind expectedKind)
        {
            var t = NextToken();
            if (t == null)
            {
                var pos = ExpressionString.Length;
                throw InternalException(pos, SpelMessage.OOD);
            }

            if (t.Kind != expectedKind)
            {
                throw InternalException(t.StartPos, SpelMessage.NOT_EXPECTED_TOKEN, expectedKind.ToString().ToLower(), t.Kind.ToString().ToLower());
            }

            return t;
        }

        private bool PeekToken(TokenKind desiredTokenKind)
        {
            return PeekToken(desiredTokenKind, false);
        }

        private bool PeekToken(TokenKind desiredTokenKind, bool consumeIfMatched)
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            if (t.Kind == desiredTokenKind)
            {
                if (consumeIfMatched)
                {
                    TokenStreamPointer++;
                }

                return true;
            }

            // Might be one of the textual forms of the operators (e.g. NE for != ) -
            // in which case we can treat it as an identifier. The list is represented here:
            // Tokenizer.alternativeOperatorNames and those ones are in order in the TokenKind enum.
            if (desiredTokenKind == TokenKind.IDENTIFIER &&
                t.Kind.Ordinal >= TokenKind.DIV.Ordinal &&
                t.Kind.Ordinal <= TokenKind.NOT.Ordinal &&
                t.Data != null)
            {
                // if t.data were null, we'd know it wasn't the textual form, it was the symbol form
                return true;
            }

            return false;
        }

        private bool PeekToken(TokenKind possible1, TokenKind possible2)
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            return t.Kind == possible1 || t.Kind == possible2;
        }

        private bool PeekToken(TokenKind possible1, TokenKind possible2, TokenKind possible3)
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            return t.Kind == possible1 || t.Kind == possible2 || t.Kind == possible3;
        }

        private Token PeekToken()
        {
            if (TokenStreamPointer >= TokenStreamLength)
            {
                return null;
            }

            return TokenStream[TokenStreamPointer];
        }

        private bool PeekIdentifierToken(string identifierString)
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            return t.Kind == TokenKind.IDENTIFIER && identifierString.Equals(t.StringValue, StringComparison.InvariantCultureIgnoreCase);
        }

        private bool PeekSelectToken()
        {
            var t = PeekToken();
            if (t == null)
            {
                return false;
            }

            return t.Kind == TokenKind.SELECT || t.Kind == TokenKind.SELECT_FIRST || t.Kind == TokenKind.SELECT_LAST;
        }

        private Token TakeToken()
        {
            if (TokenStreamPointer >= TokenStreamLength)
            {
                throw new InvalidOperationException("No token");
            }

            return TokenStream[TokenStreamPointer++];
        }

        private Token NextToken()
        {
            if (TokenStreamPointer >= TokenStreamLength)
            {
                return null;
            }

            return TokenStream[TokenStreamPointer++];
        }

        private string ToString(Token t)
        {
            if (t == null)
            {
                return string.Empty;
            }

            if (t.Kind.HasPayload)
            {
                return t.StringValue;
            }

            return t.Kind.ToString().ToLower();
        }

        private void CheckOperands(Token token, SpelNode left, SpelNode right)
        {
            CheckLeftOperand(token, left);
            CheckRightOperand(token, right);
        }

        private void CheckLeftOperand(Token token, SpelNode operandExpression)
        {
            if (operandExpression == null)
            {
                throw InternalException(token.StartPos, SpelMessage.LEFT_OPERAND_PROBLEM);
            }
        }

        private void CheckRightOperand(Token token, SpelNode operandExpression)
        {
            if (operandExpression == null)
            {
                throw InternalException(token.StartPos, SpelMessage.RIGHT_OPERAND_PROBLEM);
            }
        }

        private InternalParseException InternalException(int startPos, SpelMessage message, params object[] inserts)
        {
            return new InternalParseException(new SpelParseException(ExpressionString, startPos, message, inserts));
        }
    }
}
