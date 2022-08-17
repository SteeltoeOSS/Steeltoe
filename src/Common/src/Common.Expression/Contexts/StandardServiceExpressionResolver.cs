// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Contexts;

public class StandardServiceExpressionResolver : IServiceExpressionResolver
{
    public const string DefaultExpressionPrefix = "#{";
    public const string DefaultExpressionSuffix = "}";

    private readonly ConcurrentDictionary<string, IExpression> _expressionCache = new();
    private readonly ConcurrentDictionary<IServiceExpressionContext, IEvaluationContext> _evaluationCache = new();
    private readonly IParserContext _serviceExpressionParserContext;
    private IExpressionParser _expressionParser;
    private string _expressionPrefix = DefaultExpressionPrefix;
    private string _expressionSuffix = DefaultExpressionSuffix;

    public string ExpressionPrefix
    {
        get => _expressionPrefix;
        set
        {
            ArgumentGuard.NotNullOrEmpty(value);

            _expressionPrefix = value;
        }
    }

    public string ExpressionSuffix
    {
        get => _expressionSuffix;
        set
        {
            ArgumentGuard.NotNullOrEmpty(value);

            _expressionSuffix = value;
        }
    }

    public IExpressionParser ExpressionParser
    {
        get => _expressionParser;
        set
        {
            ArgumentGuard.NotNull(value);

            _expressionParser = value;
        }
    }

    public StandardServiceExpressionResolver()
    {
        _expressionParser = new SpelExpressionParser();
        _serviceExpressionParserContext = new ServiceExpressionParserContext(this);
    }

    public object Evaluate(string value, IServiceExpressionContext evalContext)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            _expressionCache.TryGetValue(value, out IExpression expr);

            if (expr == null)
            {
                expr = _expressionParser.ParseExpression(value, _serviceExpressionParserContext);
                _expressionCache.TryAdd(value, expr);
            }

            _evaluationCache.TryGetValue(evalContext, out IEvaluationContext sec);

            if (sec == null)
            {
                var sec2 = new StandardEvaluationContext(evalContext);
                sec2.AddPropertyAccessor(new ServiceExpressionContextAccessor());
                sec2.AddPropertyAccessor(new ServiceFactoryAccessor());
                sec2.AddPropertyAccessor(new DictionaryAccessor());
                sec2.AddPropertyAccessor(new ConfigurationAccessor());
                sec2.ServiceResolver = new ServiceFactoryResolver(evalContext.ApplicationContext);
                sec2.TypeLocator = new StandardTypeLocator();
                var conversionService = evalContext.ApplicationContext.GetService<IConversionService>();

                if (conversionService != null)
                {
                    sec2.TypeConverter = new StandardTypeConverter(conversionService);
                }

                CustomizeEvaluationContext(sec2);
                _evaluationCache.TryAdd(evalContext, sec2);
                sec = sec2;
            }

            return expr.GetValue(sec);
        }
        catch (Exception ex)
        {
            throw new ExpressionException("Expression parsing failed", ex);
        }
    }

    protected virtual void CustomizeEvaluationContext(IEvaluationContext evalContext)
    {
    }

    private sealed class ServiceExpressionParserContext : IParserContext
    {
        private readonly StandardServiceExpressionResolver _resolver;

        public bool IsTemplate => true;

        public string ExpressionPrefix => _resolver.ExpressionPrefix;

        public string ExpressionSuffix => _resolver.ExpressionSuffix;

        public ServiceExpressionParserContext(StandardServiceExpressionResolver resolver)
        {
            _resolver = resolver;
        }
    }
}
