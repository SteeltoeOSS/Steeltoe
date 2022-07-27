// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Standard;

public class SpelExpressionParser : TemplateAwareExpressionParser
{
    private readonly SpelParserOptions _configuration;

    public SpelExpressionParser()
    {
        _configuration = new SpelParserOptions();
    }

    public SpelExpressionParser(SpelParserOptions configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _configuration = configuration;
    }

    public IExpression ParseRaw(string expressionString)
    {
        return DoParseExpression(expressionString, null);
    }

    protected internal override IExpression DoParseExpression(string expressionString, IParserContext context)
    {
        return new InternalSpelExpressionParser(_configuration).DoParseExpression(expressionString, context);
    }
}