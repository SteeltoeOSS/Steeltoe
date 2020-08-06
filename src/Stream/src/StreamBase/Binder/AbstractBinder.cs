// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Expression.CSharp;
using Steeltoe.Common.Retry;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractBinder<T> : IBinder<T>
    {
        private const string GROUP_INDEX_DELIMITER = ".";

        private readonly IApplicationContext _context;
        private IEvaluationContext _evaluationContext;
        private IExpressionParser _expressionParser;

        protected AbstractBinder(IApplicationContext context)
        {
            _context = context;
        }

        public static string ApplyPrefix(string prefix, string name)
        {
            return prefix + name;
        }

        public static string ConstructDLQName(string name)
        {
            return name + ".dlq";
        }

        public abstract string ServiceName { get; set; }

        public abstract Type TargetType { get; }

        public IBinding BindConsumer(string name, string group, T inboundTarget, IConsumerOptions consumerOptions)
        {
            if (string.IsNullOrEmpty(group) && consumerOptions.IsPartitioned)
            {
                throw new ArgumentException("A consumer group is required for a partitioned subscription");
            }

            return DoBindConsumer(name, group, inboundTarget, consumerOptions);
        }

        public IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            return DoBindConsumer(name, group, (T)inboundTarget, consumerOptions);
        }

        public IBinding BindProducer(string name, T outboundTarget, IProducerOptions producerOptions)
        {
            return DoBindProducer(name, outboundTarget, producerOptions);
        }

        public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
        {
            return DoBindProducer(name, (T)outboundTarget, producerOptions);
        }

        protected abstract IBinding DoBindProducer(string name, T outboundTarget, IProducerOptions producerOptions);

        protected abstract IBinding DoBindConsumer(string name, string group, T inputTarget, IConsumerOptions consumerOptions);

        protected virtual IEvaluationContext EvaluationContext
        {
            get
            {
                if (_evaluationContext == null)
                {
                    _evaluationContext = new SimpleEvaluationContext(_context);
                }

                return _evaluationContext;
            }

            set
            {
                _evaluationContext = value;
            }
        }

        protected virtual IExpressionParser ExpressionParser
        {
            get
            {
                if (_expressionParser == null)
                {
                    _expressionParser = new ExpressionParser();
                }

                return _expressionParser;
            }

            set
            {
                _expressionParser = value;
            }
        }

        protected virtual IApplicationContext ApplicationContext
        {
            get { return _context; }
        }

        protected virtual string GroupedName(string name, string group)
        {
            return name + GROUP_INDEX_DELIMITER
                    + (!string.IsNullOrEmpty(group) ? group : "default");
        }

        protected RetryTemplate BuildRetryTemplate(IConsumerOptions options)
        {
            return new PollyRetryTemplate(GetRetryableExceptions(options.RetryableExceptions), options.MaxAttempts, options.DefaultRetryable, options.BackOffInitialInterval, options.BackOffMaxInterval, options.BackOffMultiplier);
        }

        protected Dictionary<Type, bool> GetRetryableExceptions(List<string> exceptionList)
        {
            var dict = new Dictionary<Type, bool>();
            foreach (var exception in exceptionList)
            {
                if (exception[0] == '!')
                {
                    var type = Type.GetType(exception.Substring(1), true);
                    dict.Add(type, false);
                }
                else
                {
                    dict.Add(Type.GetType(exception), true);
                }
            }

            return dict;
        }
    }
}
