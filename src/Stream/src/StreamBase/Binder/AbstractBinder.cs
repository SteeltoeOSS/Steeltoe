// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Expression.Internal.Spring.Standard;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Retry;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly: No unmanaged resources here.
    public abstract class AbstractBinder<T> : IBinder<T>
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private const string GROUP_INDEX_DELIMITER = ".";

        private readonly IApplicationContext _context;
        private readonly ILogger _logger;

        private IEvaluationContext _evaluationContext;
        private IExpressionParser _expressionParser;

        protected AbstractBinder(IApplicationContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public virtual IApplicationContext ApplicationContext
        {
            get { return _context; }
        }

        public static string ApplyPrefix(string prefix, string name)
        {
            return prefix + name;
        }

        public static string ConstructDLQName(string name)
        {
            return $"{name}.dlq";
        }

        public abstract string ServiceName { get; set; }

        public abstract Type TargetType { get; }

        public virtual IBinding BindConsumer(string name, string group, T inboundTarget, IConsumerOptions consumerOptions)
        {
            if (string.IsNullOrEmpty(group) && consumerOptions.IsPartitioned)
            {
                throw new ArgumentException("A consumer group is required for a partitioned subscription");
            }

            return DoBindConsumer(name, group, inboundTarget, consumerOptions);
        }

        public virtual IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            return DoBindConsumer(name, group, (T)inboundTarget, consumerOptions);
        }

        public virtual IBinding BindProducer(string name, T outboundTarget, IProducerOptions producerOptions)
        {
            return DoBindProducer(name, outboundTarget, producerOptions);
        }

        public virtual IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
        {
            return DoBindProducer(name, (T)outboundTarget, producerOptions);
        }

        public abstract void Dispose();

        protected abstract IBinding DoBindProducer(string name, T outboundTarget, IProducerOptions producerOptions);

        protected abstract IBinding DoBindConsumer(string name, string group, T inputTarget, IConsumerOptions consumerOptions);

        protected virtual IEvaluationContext EvaluationContext
        {
            get
            {
                _evaluationContext ??= _context.GetService<IEvaluationContext>() ?? new StandardEvaluationContext();
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
                _expressionParser ??= new SpelExpressionParser();
                return _expressionParser;
            }

            set
            {
                _expressionParser = value;
            }
        }

        protected virtual string GroupedName(string name, string group)
        {
            return name + GROUP_INDEX_DELIMITER
                    + (!string.IsNullOrEmpty(group) ? group : "default");
        }

        protected RetryTemplate BuildRetryTemplate(IConsumerOptions options)
        {
            return new PollyRetryTemplate(GetRetryableExceptions(options.RetryableExceptions), options.MaxAttempts, options.DefaultRetryable, options.BackOffInitialInterval, options.BackOffMaxInterval, options.BackOffMultiplier, _logger);
        }

        protected Dictionary<Type, bool> GetRetryableExceptions(List<string> exceptionList)
        {
            var dict = new Dictionary<Type, bool>();
            foreach (var exception in exceptionList)
            {
                if (exception[0] == '!')
                {
                    var type = Type.GetType(exception[1..], true);
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
