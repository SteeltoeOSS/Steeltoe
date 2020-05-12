// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Expression;
using Steeltoe.Integration.Retry;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder
{
    public abstract class AbstractBinder<T> : IBinder<T>
    {
        private const string GROUP_INDEX_DELIMITER = ".";

        private readonly IEvaluationContext _evaluationContext;
        private readonly IServiceProvider _serviceProvider;

        protected AbstractBinder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _evaluationContext = serviceProvider.GetService<IEvaluationContext>();
        }

        public static string ApplyPrefix(string prefix, string name)
        {
            return prefix + name;
        }

        public static string ConstructDLQName(string name)
        {
            return name + ".dlq";
        }

        public abstract string Name { get; }

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
            get { return _evaluationContext; }
        }

        protected virtual IServiceProvider ServiceProvider
        {
            get { return _serviceProvider; }
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
