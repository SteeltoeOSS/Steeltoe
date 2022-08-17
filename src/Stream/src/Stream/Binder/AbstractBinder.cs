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

namespace Steeltoe.Stream.Binder;

public abstract class AbstractBinder<T> : IBinder<T>
{
    private const string GroupIndexDelimiter = ".";

    private readonly IApplicationContext _context;
    private readonly ILogger _logger;

    private IEvaluationContext _evaluationContext;
    private IExpressionParser _expressionParser;

    protected virtual IEvaluationContext EvaluationContext
    {
        get
        {
            _evaluationContext ??= _context.GetService<IEvaluationContext>() ?? new StandardEvaluationContext();
            return _evaluationContext;
        }
        set => _evaluationContext = value;
    }

    protected virtual IExpressionParser ExpressionParser
    {
        get
        {
            _expressionParser ??= new SpelExpressionParser();
            return _expressionParser;
        }
        set => _expressionParser = value;
    }

    public virtual IApplicationContext ApplicationContext => _context;

    public abstract string ServiceName { get; set; }

    public abstract Type TargetType { get; }

    protected AbstractBinder(IApplicationContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public static string ApplyPrefix(string prefix, string name)
    {
        return prefix + name;
    }

    public static string ConstructDlqName(string name)
    {
        return $"{name}.dlq";
    }

    public virtual IBinding BindConsumer(string name, string group, T inboundTarget, IConsumerOptions consumerOptions)
    {
        if (string.IsNullOrEmpty(group) && consumerOptions.IsPartitioned)
        {
            throw new ArgumentException("A consumer group is required for a partitioned subscription.", nameof(group));
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected abstract IBinding DoBindProducer(string name, T outboundTarget, IProducerOptions producerOptions);

    protected abstract IBinding DoBindConsumer(string name, string group, T inputTarget, IConsumerOptions consumerOptions);

    protected virtual string GroupedName(string name, string group)
    {
        return name + GroupIndexDelimiter + (!string.IsNullOrEmpty(group) ? group : "default");
    }

    protected RetryTemplate BuildRetryTemplate(IConsumerOptions options)
    {
        return new PollyRetryTemplate(GetRetryableExceptions(options.RetryableExceptions), options.MaxAttempts, options.DefaultRetryable,
            options.BackOffInitialInterval, options.BackOffMaxInterval, options.BackOffMultiplier, _logger);
    }

    protected Dictionary<Type, bool> GetRetryableExceptions(List<string> exceptionList)
    {
        var dict = new Dictionary<Type, bool>();

        foreach (string exception in exceptionList)
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
