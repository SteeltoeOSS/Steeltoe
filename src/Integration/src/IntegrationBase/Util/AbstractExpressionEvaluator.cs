// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Expression.Spring.Standard;
using Steeltoe.Common.Expression.Spring.Support;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Util
{
    public abstract class AbstractExpressionEvaluator
    {
        private IEvaluationContext _evaluationContext;

        private IMessageBuilderFactory _messageBuilderFactory;

        private IIntegrationServices _integrationServices;

        public IExpressionParser ExpressionParser { get; } = new SpelExpressionParser();

        public IEvaluationContext EvaluationContext
        {
            get
            {
                if (_evaluationContext == null)
                {
                    _evaluationContext = GetEvaluationContext();
                }

                return _evaluationContext;
            }

            set
            {
                _evaluationContext = value;
            }
        }

        public IIntegrationServices IntegrationServices
        {
            get
            {
                if (_integrationServices == null)
                {
                    _integrationServices = IntegrationServicesUtils.GetIntegrationServices(ApplicationContext);
                }

                return _integrationServices;
            }
        }

        public IMessageBuilderFactory MessageBuilderFactory
        {
            get
            {
                if (_messageBuilderFactory == null)
                {
                    _messageBuilderFactory = GetMessageBuilderFactory();
                }

                return _messageBuilderFactory;
            }

            set
            {
                _messageBuilderFactory = value;
            }
        }

        public ITypeConverter TypeConverter { get; set; } = new BeanFactoryTypeConverter();

        public IApplicationContext ApplicationContext { get; }

        protected AbstractExpressionEvaluator(IApplicationContext context)
        {
            ApplicationContext = context;
        }

        protected virtual IMessageBuilderFactory GetMessageBuilderFactory()
        {
            return IntegrationServices.MessageBuilderFactory;
        }

        protected virtual IEvaluationContext GetEvaluationContext(bool contextRequired = true)
        {
            if (_evaluationContext == null)
            {
                if (ApplicationContext == null && !contextRequired)
                {
                    _evaluationContext = new StandardEvaluationContext();
                }
                else
                {
                    _evaluationContext = new StandardEvaluationContext(ApplicationContext);
                }

                ((StandardEvaluationContext)_evaluationContext).TypeConverter = TypeConverter;

                if (ApplicationContext != null)
                {
                    var conversionService = ApplicationContext.GetService<IConversionService>(IntegrationUtils.INTEGRATION_CONVERSION_SERVICE_BEAN_NAME);
                    if (conversionService != null)
                    {
                        TypeConverter.ConversionService = conversionService;
                    }
                }
            }

            return _evaluationContext;
        }

        protected T EvaluateExpression<T>(IExpression expression, IMessage message)
        {
            return (T)EvaluateExpression(expression, message, typeof(T));
        }

        protected object EvaluateExpression(IExpression expression, IMessage message, Type expectedType)
        {
            try
            {
                return EvaluateExpression(expression, (object)message, expectedType);
            }
            catch (Exception ex)
            {
                Exception cause = null;
                if (ex is EvaluationException)
                { // NOSONAR
                    cause = ex.InnerException;
                }

                var wrapped = IntegrationServicesUtils.WrapInHandlingExceptionIfNecessary(message, "Expression evaluation failed: " + expression.ExpressionString, cause ?? ex);
                if (wrapped != ex)
                {
                    throw wrapped;
                }

                throw;
            }
        }

        protected object EvaluateExpression(string expression, object input)
        {
            return EvaluateExpression(expression, input, null);
        }

        protected object EvaluateExpression(string expression, object input, Type expectedType)
        {
            return ExpressionParser.ParseExpression(expression).GetValue(EvaluationContext, input, expectedType);
        }

        protected object EvaluateExpression(IExpression expression)
        {
            return expression.GetValue(EvaluationContext);
        }

        protected object EvaluateExpression(IExpression expression, object input, Type expectedType)
        {
            return expression.GetValue(EvaluationContext, input, expectedType);
        }

        protected T EvaluateExpression<T>(IExpression expression, object input)
        {
            return (T)EvaluateExpression(expression, input, typeof(T));
        }
    }
}
