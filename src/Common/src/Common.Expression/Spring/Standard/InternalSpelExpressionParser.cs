// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text.RegularExpressions;
using Steeltoe.Common.Expression.Internal.Spring.Ast;
using Steeltoe.Common.Expression.Internal.Spring.Common;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class InternalSpelExpressionParser : TemplateAwareExpressionParser
{
    private static readonly Regex ValidQualifiedIdPattern = new("[\\p{L}\\p{N}_$]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    internal Stack<SpelNode> ConstructedNodes = new();

    internal SpelParserOptions Configuration { get; }

    internal string ExpressionString { get; private set; }

    internal List<Token> TokenStream { get; private set; } = new();

    internal int TokenStreamLength { get; private set; }

    internal int TokenStreamPointer { get; private set; }

    public InternalSpelExpressionParser(SpelParserOptions configuration)
    {
        Configuration = configuration;
    }

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
            SpelNode ast = EatExpression();

            if (ast == null)
            {
                throw new InvalidOperationException("No node");
            }

            Token t = PeekToken();

            if (t != null)
            {
                throw new SpelParseException(t.StartPos, SpelMessage.MoreInput, ToString(NextToken()));
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
        SpelNode expr = EatLogicalOrExpression();
        Token t = PeekToken();

        if (t != null)
        {
            if (Equals(t.Kind, TokenKind.Assign))
            {
                // a=b
                expr ??= new NullLiteral(t.StartPos - 1, t.EndPos - 1);

                NextToken();
                SpelNode assignedValue = EatLogicalOrExpression();
                return new Assign(t.StartPos, t.EndPos, expr, assignedValue);
            }

            if (Equals(t.Kind, TokenKind.Elvis))
            {
                // a?:b (a if it isn't null, otherwise b)
                expr ??= new NullLiteral(t.StartPos - 1, t.EndPos - 2);

                NextToken(); // elvis has left the building
                SpelNode valueIfNull = EatExpression() ?? new NullLiteral(t.StartPos + 1, t.EndPos + 1);

                return new Elvis(t.StartPos, t.EndPos, expr, valueIfNull);
            }

            if (Equals(t.Kind, TokenKind.QuestionMark))
            {
                // a?b:c
                expr ??= new NullLiteral(t.StartPos - 1, t.EndPos - 1);

                NextToken();
                SpelNode ifTrueExprValue = EatExpression();
                EatToken(TokenKind.Colon);
                SpelNode ifFalseExprValue = EatExpression();
                return new Ternary(t.StartPos, t.EndPos, expr, ifTrueExprValue, ifFalseExprValue);
            }
        }

        return expr;
    }

    private SpelNode EatLogicalOrExpression()
    {
        SpelNode expr = EatLogicalAndExpression();

        while (PeekIdentifierToken("or") || PeekToken(TokenKind.SymbolicOr))
        {
            // consume OR
            Token t = TakeToken();
            SpelNode rhExpr = EatLogicalAndExpression();
            CheckOperands(t, expr, rhExpr);
            expr = new OpOr(t.StartPos, t.EndPos, expr, rhExpr);
        }

        return expr;
    }

    private SpelNode EatLogicalAndExpression()
    {
        SpelNode expr = EatRelationalExpression();

        while (PeekIdentifierToken("and") || PeekToken(TokenKind.SymbolicAnd))
        {
            // consume 'AND'
            Token t = TakeToken();
            SpelNode rhExpr = EatRelationalExpression();
            CheckOperands(t, expr, rhExpr);
            expr = new OpAnd(t.StartPos, t.EndPos, expr, rhExpr);
        }

        return expr;
    }

    private SpelNode EatRelationalExpression()
    {
        SpelNode expr = EatSumExpression();
        Token relationalOperatorToken = MaybeEatRelationalOperator();

        if (relationalOperatorToken != null)
        {
            // consume relational operator token
            Token t = TakeToken();
            SpelNode rhExpr = EatSumExpression();
            CheckOperands(t, expr, rhExpr);
            TokenKind tk = relationalOperatorToken.Kind;

            if (relationalOperatorToken.IsNumericRelationalOperator)
            {
                if (Equals(tk, TokenKind.Gt))
                {
                    return new OpGt(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (Equals(tk, TokenKind.Lt))
                {
                    return new OpLt(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (Equals(tk, TokenKind.Le))
                {
                    return new OpLe(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (Equals(tk, TokenKind.Ge))
                {
                    return new OpGe(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (Equals(tk, TokenKind.Eq))
                {
                    return new OpEq(t.StartPos, t.EndPos, expr, rhExpr);
                }

                if (!Equals(tk, TokenKind.Ne))
                {
                    throw new InvalidOperationException("Not-equals token expected");
                }

                return new OpNe(t.StartPos, t.EndPos, expr, rhExpr);
            }

            if (Equals(tk, TokenKind.InstanceOf))
            {
                return new OperatorInstanceOf(t.StartPos, t.EndPos, expr, rhExpr);
            }

            if (Equals(tk, TokenKind.Matches))
            {
                return new OperatorMatches(t.StartPos, t.EndPos, expr, rhExpr);
            }

            if (!Equals(tk, TokenKind.Between))
            {
                throw new InvalidOperationException("Between token expected");
            }

            return new OperatorBetween(t.StartPos, t.EndPos, expr, rhExpr);
        }

        return expr;
    }

    private SpelNode EatSumExpression()
    {
        SpelNode expr = EatProductExpression();

        while (PeekToken(TokenKind.Plus, TokenKind.Minus, TokenKind.Inc))
        {
            // consume PLUS or MINUS or INC
            Token t = TakeToken();
            SpelNode rhExpr = EatProductExpression();
            CheckRightOperand(t, rhExpr);

            if (Equals(t.Kind, TokenKind.Plus))
            {
                expr = new OpPlus(t.StartPos, t.EndPos, expr, rhExpr);
            }
            else if (Equals(t.Kind, TokenKind.Minus))
            {
                expr = new OpMinus(t.StartPos, t.EndPos, expr, rhExpr);
            }
        }

        return expr;
    }

    private SpelNode EatProductExpression()
    {
        SpelNode expr = EatPowerIncDecExpression();

        while (PeekToken(TokenKind.Star, TokenKind.Div, TokenKind.Mod))
        {
            Token t = TakeToken(); // consume STAR/DIV/MOD
            SpelNode rhExpr = EatPowerIncDecExpression();
            CheckOperands(t, expr, rhExpr);

            if (Equals(t.Kind, TokenKind.Star))
            {
                expr = new OpMultiply(t.StartPos, t.EndPos, expr, rhExpr);
            }
            else if (Equals(t.Kind, TokenKind.Div))
            {
                expr = new OpDivide(t.StartPos, t.EndPos, expr, rhExpr);
            }
            else
            {
                if (!Equals(t.Kind, TokenKind.Mod))
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
        SpelNode expr = EatUnaryExpression();

        if (PeekToken(TokenKind.Power))
        {
            Token t = TakeToken(); // consume POWER
            SpelNode rhExpr = EatUnaryExpression();
            CheckRightOperand(t, rhExpr);
            return new OperatorPower(t.StartPos, t.EndPos, expr, rhExpr);
        }

        if (expr != null && PeekToken(TokenKind.Inc, TokenKind.Dec))
        {
            Token t = TakeToken(); // consume INC/DEC

            if (Equals(t.Kind, TokenKind.Inc))
            {
                return new OpInc(t.StartPos, t.EndPos, true, expr);
            }

            return new OpDec(t.StartPos, t.EndPos, true, expr);
        }

        return expr;
    }

    private SpelNode EatUnaryExpression()
    {
        if (PeekToken(TokenKind.Plus, TokenKind.Minus, TokenKind.Not))
        {
            Token t = TakeToken();
            SpelNode expr = EatUnaryExpression();

            if (expr == null)
            {
                throw new InvalidOperationException("No node");
            }

            if (Equals(t.Kind, TokenKind.Not))
            {
                return new OperatorNot(t.StartPos, t.EndPos, expr);
            }

            if (Equals(t.Kind, TokenKind.Plus))
            {
                return new OpPlus(t.StartPos, t.EndPos, expr);
            }

            if (!Equals(t.Kind, TokenKind.Minus))
            {
                throw new InvalidOperationException("Minus token expected");
            }

            return new OpMinus(t.StartPos, t.EndPos, expr);
        }

        if (PeekToken(TokenKind.Inc, TokenKind.Dec))
        {
            Token t = TakeToken();
            SpelNode expr = EatUnaryExpression();

            if (Equals(t.Kind, TokenKind.Inc))
            {
                return new OpInc(t.StartPos, t.EndPos, false, expr);
            }

            return new OpDec(t.StartPos, t.EndPos, false, expr);
        }

        return EatPrimaryExpression();
    }

    private SpelNode EatPrimaryExpression()
    {
        SpelNode start = EatStartNode(); // always a start node
        List<SpelNode> nodes = null;
        SpelNode node = EatNode();

        while (node != null)
        {
            nodes ??= new List<SpelNode>(4)
            {
                start
            };

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
        return PeekToken(TokenKind.Dot, TokenKind.SafeNavigator) ? EatDottedNode() : EatNonDottedNode();
    }

    private SpelNode EatNonDottedNode()
    {
        if (PeekToken(TokenKind.LeftSquare) && MaybeEatIndexer())
        {
            return Pop();
        }

        return null;
    }

    private SpelNode EatDottedNode()
    {
        Token t = TakeToken(); // it was a '.' or a '?.'
        bool nullSafeNavigation = Equals(t.Kind, TokenKind.SafeNavigator);

        if (MaybeEatMethodOrProperty(nullSafeNavigation) || MaybeEatFunctionOrVar() || MaybeEatProjection(nullSafeNavigation) ||
            MaybeEatSelection(nullSafeNavigation))
        {
            return Pop();
        }

        if (PeekToken() == null)
        {
            // unexpectedly ran out of data
            throw InternalException(t.StartPos, SpelMessage.Ood);
        }

        throw InternalException(t.StartPos, SpelMessage.UnexpectedDataAfterDot, ToString(PeekToken()));
    }

    private bool MaybeEatFunctionOrVar()
    {
        if (!PeekToken(TokenKind.Hash))
        {
            return false;
        }

        Token t = TakeToken();
        Token functionOrVariableName = EatToken(TokenKind.Identifier);
        SpelNode[] args = MaybeEatMethodArgs();

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
        if (!PeekToken(TokenKind.LeftParen))
        {
            return null;
        }

        var args = new List<SpelNode>();
        ConsumeArguments(args);
        EatToken(TokenKind.RightParen);
        return args.ToArray();
    }

    private void EatConstructorArgs(List<SpelNode> accumulatedArguments)
    {
        if (!PeekToken(TokenKind.LeftParen))
        {
            throw new InternalParseException(new SpelParseException(ExpressionString, PositionOf(PeekToken()), SpelMessage.MissingConstructorArgs));
        }

        ConsumeArguments(accumulatedArguments);
        EatToken(TokenKind.RightParen);
    }

    private void ConsumeArguments(List<SpelNode> accumulatedArguments)
    {
        Token t = PeekToken();

        if (t == null)
        {
            throw new InvalidOperationException("Expected token");
        }

        int pos = t.StartPos;
        Token next;

        do
        {
            NextToken(); // consume (first time through) or comma (subsequent times)
            t = PeekToken();

            if (t == null)
            {
                throw InternalException(pos, SpelMessage.RunOutOfArguments);
            }

            if (!Equals(t.Kind, TokenKind.RightParen))
            {
                accumulatedArguments.Add(EatExpression());
            }

            next = PeekToken();
        }
        while (next != null && Equals(next.Kind, TokenKind.Comma));

        if (next == null)
        {
            throw InternalException(pos, SpelMessage.RunOutOfArguments);
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
        bool hasEaten = MaybeEatLiteral();
        hasEaten = hasEaten || MaybeEatParenExpression();
        hasEaten = hasEaten || MaybeEatTypeReference();
        hasEaten = hasEaten || MaybeEatNullReference();
        hasEaten = hasEaten || MaybeEatConstructorReference();
        hasEaten = hasEaten || MaybeEatMethodOrProperty(false);
        hasEaten = hasEaten || MaybeEatFunctionOrVar();
        hasEaten = hasEaten || MaybeEatServiceReference();
        hasEaten = hasEaten || MaybeEatProjection(false);
        hasEaten = hasEaten || MaybeEatSelection(false);
        hasEaten = hasEaten || MaybeEatIndexer();
        hasEaten = hasEaten || MaybeEatInlineListOrMap();

        return hasEaten ? Pop() : null;
    }

    private bool MaybeEatServiceReference()
    {
        if (PeekToken(TokenKind.ServiceRef) || PeekToken(TokenKind.FactoryServiceRef))
        {
            Token serviceRefToken = TakeToken();
            Token serviceNameToken = null;
            string serviceName = null;

            if (PeekToken(TokenKind.Identifier))
            {
                serviceNameToken = EatToken(TokenKind.Identifier);
                serviceName = serviceNameToken.StringValue;
            }
            else if (PeekToken(TokenKind.LiteralString))
            {
                serviceNameToken = EatToken(TokenKind.LiteralString);
                serviceName = serviceNameToken.StringValue;
                serviceName = serviceName.Substring(1, serviceName.Length - 1 - 1);
            }
            else
            {
                throw InternalException(serviceRefToken.StartPos, SpelMessage.InvalidServiceReference);
            }

            ServiceReference serviceReference;

            if (Equals(serviceRefToken.Kind, TokenKind.FactoryServiceRef))
            {
                string serviceNameString = new string(TokenKind.FactoryServiceRef.TokenChars) + serviceName;
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
        if (PeekToken(TokenKind.Identifier))
        {
            Token typeName = PeekToken();

            if (typeName == null)
            {
                throw new InvalidOperationException("Expected token");
            }

            if (!"T".Equals(typeName.StringValue))
            {
                return false;
            }

            // It looks like a type reference but is T being used as a map key?
            Token t = TakeToken();

            if (PeekToken(TokenKind.RightSquare))
            {
                // looks like 'T]' (T is map key)
                Push(new PropertyOrFieldReference(false, t.StringValue, t.StartPos, t.EndPos));
                return true;
            }

            EatToken(TokenKind.LeftParen);
            SpelNode node = EatPossiblyQualifiedId();

            // dotted qualified id
            // Are there array dimensions?
            int dims = 0;

            while (PeekToken(TokenKind.LeftSquare, true))
            {
                EatToken(TokenKind.RightSquare);
                dims++;
            }

            EatToken(TokenKind.RightParen);
            ConstructedNodes.Push(new TypeReference(typeName.StartPos, typeName.EndPos, node, dims));
            return true;
        }

        return false;
    }

    private bool MaybeEatNullReference()
    {
        if (PeekToken(TokenKind.Identifier))
        {
            Token nullToken = PeekToken();

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
        Token t = PeekToken();

        if (!PeekToken(TokenKind.Project, true))
        {
            return false;
        }

        if (t == null)
        {
            throw new InvalidOperationException("No token");
        }

        SpelNode expr = EatExpression();

        if (expr == null)
        {
            throw new InvalidOperationException("No node");
        }

        EatToken(TokenKind.RightSquare);
        ConstructedNodes.Push(new Projection(nullSafeNavigation, t.StartPos, t.EndPos, expr));
        return true;
    }

    private bool MaybeEatInlineListOrMap()
    {
        Token t = PeekToken();

        if (!PeekToken(TokenKind.LeftCurly, true))
        {
            return false;
        }

        if (t == null)
        {
            throw new InvalidOperationException("No token");
        }

        SpelNode expr = null;
        Token closingCurly = PeekToken();

        if (PeekToken(TokenKind.RightCurly, true))
        {
            // empty list '{}'
            if (closingCurly == null)
            {
                throw new InvalidOperationException("No token");
            }

            expr = new InlineList(t.StartPos, closingCurly.EndPos);
        }
        else if (PeekToken(TokenKind.Colon, true))
        {
            closingCurly = EatToken(TokenKind.RightCurly);

            // empty map '{:}'
            expr = new InlineMap(t.StartPos, closingCurly.EndPos);
        }
        else
        {
            SpelNode firstExpression = EatExpression();

            // Next is either:
            // '}' - end of list
            // ',' - more expressions in this list
            // ':' - this is a map!
            if (PeekToken(TokenKind.RightCurly))
            {
                // list with one item in it
                var elements = new List<SpelNode>
                {
                    firstExpression
                };

                closingCurly = EatToken(TokenKind.RightCurly);
                expr = new InlineList(t.StartPos, closingCurly.EndPos, elements.ToArray());
            }
            else if (PeekToken(TokenKind.Comma, true))
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
                while (PeekToken(TokenKind.Comma, true));

                closingCurly = EatToken(TokenKind.RightCurly);
                expr = new InlineList(t.StartPos, closingCurly.EndPos, elements.ToArray());
            }
            else if (PeekToken(TokenKind.Colon, true))
            {
                // map!
                var elements = new List<SpelNode>
                {
                    firstExpression,
                    EatExpression()
                };

                while (PeekToken(TokenKind.Comma, true))
                {
                    elements.Add(EatExpression());
                    EatToken(TokenKind.Colon);
                    elements.Add(EatExpression());
                }

                closingCurly = EatToken(TokenKind.RightCurly);
                expr = new InlineMap(t.StartPos, closingCurly.EndPos, elements.ToArray());
            }
            else
            {
                throw InternalException(t.StartPos, SpelMessage.Ood);
            }
        }

        ConstructedNodes.Push(expr);
        return true;
    }

    private bool MaybeEatIndexer()
    {
        Token t = PeekToken();

        if (!PeekToken(TokenKind.LeftSquare, true))
        {
            return false;
        }

        if (t == null)
        {
            throw new InvalidOperationException("No token");
        }

        SpelNode expr = EatExpression();

        if (expr == null)
        {
            throw new InvalidOperationException("No node");
        }

        EatToken(TokenKind.RightSquare);
        ConstructedNodes.Push(new Indexer(t.StartPos, t.EndPos, expr));
        return true;
    }

    private bool MaybeEatSelection(bool nullSafeNavigation)
    {
        Token t = PeekToken();

        if (!PeekSelectToken())
        {
            return false;
        }

        if (t == null)
        {
            throw new InvalidOperationException("No token");
        }

        NextToken();
        SpelNode expr = EatExpression();

        if (expr == null)
        {
            throw InternalException(t.StartPos, SpelMessage.MissingSelectionExpression);
        }

        EatToken(TokenKind.RightSquare);

        if (Equals(t.Kind, TokenKind.SelectFirst))
        {
            ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.First, t.StartPos, t.EndPos, expr));
        }
        else if (Equals(t.Kind, TokenKind.SelectLast))
        {
            ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.Last, t.StartPos, t.EndPos, expr));
        }
        else
        {
            ConstructedNodes.Push(new Selection(nullSafeNavigation, Selection.All, t.StartPos, t.EndPos, expr));
        }

        return true;
    }

    private SpelNode EatPossiblyQualifiedId()
    {
        var qualifiedIdPieces = new List<SpelNode>();
        Token node = PeekToken();

        while (IsValidQualifiedId(node))
        {
            NextToken();

            if (!Equals(node.Kind, TokenKind.Dot))
            {
                qualifiedIdPieces.Add(new Identifier(node.StringValue, node.StartPos, node.EndPos));
            }

            node = PeekToken();
        }

        if (qualifiedIdPieces.Count == 0)
        {
            if (node == null)
            {
                throw InternalException(ExpressionString.Length, SpelMessage.Ood);
            }

            throw InternalException(node.StartPos, SpelMessage.NotExpectedToken, "qualified ID", node.Kind.ToString().ToLower());
        }

        return new QualifiedIdentifier(qualifiedIdPieces.First().StartPosition, qualifiedIdPieces.Last().EndPosition, qualifiedIdPieces.ToArray());
    }

    private bool IsValidQualifiedId(Token node)
    {
        if (node == null || Equals(node.Kind, TokenKind.LiteralString))
        {
            return false;
        }

        if (Equals(node.Kind, TokenKind.Dot) || Equals(node.Kind, TokenKind.Identifier))
        {
            return true;
        }

        string value = node.StringValue;
        return !string.IsNullOrEmpty(value) && ValidQualifiedIdPattern.Matches(value).Count > 0;
    }

    private bool MaybeEatMethodOrProperty(bool nullSafeNavigation)
    {
        if (PeekToken(TokenKind.Identifier))
        {
            Token methodOrPropertyName = TakeToken();
            SpelNode[] args = MaybeEatMethodArgs();

            if (args == null)
            {
                // property
                Push(new PropertyOrFieldReference(nullSafeNavigation, methodOrPropertyName.StringValue, methodOrPropertyName.StartPos,
                    methodOrPropertyName.EndPos));

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
            Token newToken = TakeToken();

            // It looks like a constructor reference but is NEW being used as a map key?
            if (PeekToken(TokenKind.RightSquare))
            {
                // looks like 'NEW]' (so NEW used as map key)
                Push(new PropertyOrFieldReference(false, newToken.StringValue, newToken.StartPos, newToken.EndPos));
                return true;
            }

            SpelNode possiblyQualifiedConstructorName = EatPossiblyQualifiedId();

            var nodes = new List<SpelNode>
            {
                possiblyQualifiedConstructorName
            };

            if (PeekToken(TokenKind.LeftSquare))
            {
                // array initializer
                var dimensions = new List<SpelNode>();

                while (PeekToken(TokenKind.LeftSquare, true))
                {
                    dimensions.Add(!PeekToken(TokenKind.RightSquare) ? EatExpression() : null);

                    EatToken(TokenKind.RightSquare);
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
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        if (Equals(t.Kind, TokenKind.LiteralInt))
        {
            Push(Literal.GetIntLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.Integer));
        }
        else if (Equals(t.Kind, TokenKind.LiteralLong))
        {
            Push(Literal.GetLongLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.Integer));
        }
        else if (Equals(t.Kind, TokenKind.LiteralHexInt))
        {
            Push(Literal.GetIntLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.HexNumber));
        }
        else if (Equals(t.Kind, TokenKind.LiteralHexLong))
        {
            Push(Literal.GetLongLiteral(t.StringValue, t.StartPos, t.EndPos, NumberStyles.HexNumber));
        }
        else if (Equals(t.Kind, TokenKind.LiteralReal))
        {
            Push(Literal.GetRealLiteral(t.StringValue, t.StartPos, t.EndPos, false));
        }
        else if (Equals(t.Kind, TokenKind.LiteralRealFloat))
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
        else if (Equals(t.Kind, TokenKind.LiteralString))
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
        if (PeekToken(TokenKind.LeftParen))
        {
            NextToken();
            SpelNode expr = EatExpression();

            if (expr == null)
            {
                throw new InvalidOperationException("No node");
            }

            EatToken(TokenKind.RightParen);
            Push(expr);
            return true;
        }

        return false;
    }

    private Token MaybeEatRelationalOperator()
    {
        Token t = PeekToken();

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
            string idString = t.StringValue;

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
        Token t = NextToken();

        if (t == null)
        {
            int pos = ExpressionString.Length;
            throw InternalException(pos, SpelMessage.Ood);
        }

        if (!Equals(t.Kind, expectedKind))
        {
            throw InternalException(t.StartPos, SpelMessage.NotExpectedToken, expectedKind.ToString().ToLower(), t.Kind.ToString().ToLower());
        }

        return t;
    }

    private bool PeekToken(TokenKind desiredTokenKind)
    {
        return PeekToken(desiredTokenKind, false);
    }

    private bool PeekToken(TokenKind desiredTokenKind, bool consumeIfMatched)
    {
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        if (Equals(t.Kind, desiredTokenKind))
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
        if (Equals(desiredTokenKind, TokenKind.Identifier) && t.Kind.Ordinal >= TokenKind.Div.Ordinal && t.Kind.Ordinal <= TokenKind.Not.Ordinal &&
            t.Data != null)
        {
            // if t.data were null, we'd know it wasn't the textual form, it was the symbol form
            return true;
        }

        return false;
    }

    private bool PeekToken(TokenKind possible1, TokenKind possible2)
    {
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        return Equals(t.Kind, possible1) || Equals(t.Kind, possible2);
    }

    private bool PeekToken(TokenKind possible1, TokenKind possible2, TokenKind possible3)
    {
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        return Equals(t.Kind, possible1) || Equals(t.Kind, possible2) || Equals(t.Kind, possible3);
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
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        return Equals(t.Kind, TokenKind.Identifier) && identifierString.Equals(t.StringValue, StringComparison.InvariantCultureIgnoreCase);
    }

    private bool PeekSelectToken()
    {
        Token t = PeekToken();

        if (t == null)
        {
            return false;
        }

        return Equals(t.Kind, TokenKind.Select) || Equals(t.Kind, TokenKind.SelectFirst) || Equals(t.Kind, TokenKind.SelectLast);
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
            throw InternalException(token.StartPos, SpelMessage.LeftOperandProblem);
        }
    }

    private void CheckRightOperand(Token token, SpelNode operandExpression)
    {
        if (operandExpression == null)
        {
            throw InternalException(token.StartPos, SpelMessage.RightOperandProblem);
        }
    }

    private InternalParseException InternalException(int startPos, SpelMessage message, params object[] inserts)
    {
        return new InternalParseException(new SpelParseException(ExpressionString, startPos, message, inserts));
    }
}
