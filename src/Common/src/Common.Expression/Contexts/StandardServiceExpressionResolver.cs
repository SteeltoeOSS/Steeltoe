// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.Common.Expression.Internal.Contexts
{
    public class StandardServiceExpressionResolver : IServiceExpressionResolver
    {
        public static readonly string DEFAULT_EXPRESSION_PREFIX = "#{";
        public static readonly string DEFAULT_EXPRESSION_SUFFIX = "}";

        private readonly ConcurrentDictionary<string, IExpression> _expressionCache = new ();
        private readonly ConcurrentDictionary<IServiceExpressionContext, IEvaluationContext> _evaluationCache = new ();
        private readonly IParserContext _serviceExpressionParserContext;
        private IExpressionParser _expressionParser;
        private string _expressionPrefix = DEFAULT_EXPRESSION_PREFIX;
        private string _expressionSuffix = DEFAULT_EXPRESSION_SUFFIX;

        public StandardServiceExpressionResolver()
        {
            _expressionParser = new SpelExpressionParser();
            _serviceExpressionParserContext = new ServiceExpressionParserContext(this);
        }

        public string ExpressionPrefix
        {
            get => _expressionPrefix;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Expression prefix must not be empty");
                }

                _expressionPrefix = value;
            }
        }

        public string ExpressionSuffix
        {
            get => _expressionSuffix;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Expression suffix must not be empty");
                }

                _expressionSuffix = value;
            }
        }

        public IExpressionParser ExpressionParser
        {
            get => _expressionParser;
            set
            {
                _expressionParser = value ?? throw new ArgumentException("Expression parser must not be null");
            }
        }

        public object Evaluate(string value, IServiceExpressionContext evalContext)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            try
            {
                _expressionCache.TryGetValue(value, out var expr);
                if (expr == null)
                {
                    expr = _expressionParser.ParseExpression(value, _serviceExpressionParserContext);
                    _expressionCache.TryAdd(value, expr);
                }

                _evaluationCache.TryGetValue(evalContext, out var sec);
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

        private class ServiceExpressionParserContext : IParserContext
        {
            private readonly StandardServiceExpressionResolver _resolver;

            public ServiceExpressionParserContext(StandardServiceExpressionResolver resolver)
            {
                _resolver = resolver;
            }

            public bool IsTemplate => true;

            public string ExpressionPrefix => _resolver.ExpressionPrefix;

            public string ExpressionSuffix => _resolver.ExpressionSuffix;
        }
    }
}
