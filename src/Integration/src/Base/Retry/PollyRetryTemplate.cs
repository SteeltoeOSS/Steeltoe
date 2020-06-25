// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Retry
{
    public class PollyRetryTemplate : RetryTemplate
    {
        private const string RECOVERY_CALLBACK_KEY = "PollyRetryTemplate.RecoveryCallback";
        private const string RETRYCONTEXT_KEY = "PollyRetryTemplate.RetryContext";

        private const string RECOVERED = "context.recovered";
        private const string CLOSED = "context.closed";
        private const string RECOVERED_RESULT = "context.recovered.result";

        private readonly BinaryExceptionClassifier _retryableExceptions;
        private readonly int _maxAttempts;
        private readonly int _backOffInitialInterval;
        private readonly int _backOffMaxInterval;
        private readonly double _backOffMultiplier;

        public PollyRetryTemplate(Dictionary<Type, bool> retryableExceptions, int maxAttempts, bool defaultRetryable, int backOffInitialInterval, int backOffMaxInterval, double backOffMultiplier)
        {
            _retryableExceptions = new BinaryExceptionClassifier(retryableExceptions, defaultRetryable);
            _maxAttempts = maxAttempts;
            _backOffInitialInterval = backOffInitialInterval;
            _backOffMaxInterval = backOffMaxInterval;
            _backOffMultiplier = backOffMultiplier;
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback)
        {
            return Execute<T>(retryCallback, null);
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback)
        {
            var policy = BuildPolicy<T>();
            var retryContext = new RetryContext();
            var context = new Context();

            context.Add(RETRYCONTEXT_KEY, retryContext);
            if (recoveryCallback != null)
            {
                retryContext.SetAttribute(RECOVERY_CALLBACK_KEY, recoveryCallback);
            }

            CallListenerOpen(retryContext);
            var result = policy.Execute(
                (ctx) =>
                {
                    var callbackResult = retryCallback(retryContext);
                    if (recoveryCallback != null)
                    {
                        var recovered = (bool)retryContext.GetAttribute(RECOVERED);
                        if (recovered)
                        {
                            callbackResult = (T)retryContext.GetAttribute(RECOVERED_RESULT);
                        }
                    }

                    return callbackResult;
                }, context);

            CallListenerClose(retryContext, retryContext.LastException);
            return result;
        }

        public override void Execute(Action<IRetryContext> retryCallback)
        {
            Execute(retryCallback, null);
        }

        public override void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback)
        {
            var policy = BuildPolicy<object>();
            var retryContext = new RetryContext();
            var context = new Context();

            context.Add(RETRYCONTEXT_KEY, retryContext);
            if (recoveryCallback != null)
            {
                retryContext.SetAttribute(RECOVERY_CALLBACK_KEY, recoveryCallback);
            }

            if (!CallListenerOpen(retryContext))
            {
                throw new TerminatedRetryException("Retry terminated abnormally by interceptor before first attempt");
            }

            policy.Execute(
                (ctx) =>
            {
                retryCallback(retryContext);
                return null;
            }, context);

            CallListenerClose(retryContext, retryContext.LastException);
        }

        private Policy<T> BuildPolicy<T>()
        {
            var delay = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(_backOffInitialInterval), _maxAttempts - 1, _backOffMultiplier, true);
            var retryPolicy = Policy<T>.HandleInner<Exception>((e) =>
            {
                return _retryableExceptions.Classify(e);
            })
            .WaitAndRetry(delay, (delegateResult, time, count, context) => OnRetry(delegateResult, time, count, context));

            var fallbackPolicy = Policy<T>.Handle<Exception>()
                   .Fallback<T>(
                        (delegateResult, context, token) =>
                        {
                            var retryContext = GetRetryContext(context);
                            retryContext.LastException = delegateResult.Exception;
                            var callback = retryContext.GetAttribute(RECOVERY_CALLBACK_KEY) as IRecoveryCallback;
                            var result = default(T);
                            if (callback != null)
                            {
                                result = (T)callback.Recover(retryContext);
                                retryContext.SetAttribute(RECOVERED, true);
                                retryContext.SetAttribute(RECOVERED_RESULT, result);
                            }

                            return result;
                        }, (ex, context) =>
                        {
                        });

            return fallbackPolicy.Wrap(retryPolicy);
        }

        private RetryContext GetRetryContext(Context context)
        {
            if (context.TryGetValue(RETRYCONTEXT_KEY, out var obj))
            {
                return (RetryContext)obj;
            }
            else
            {
                return new RetryContext();
            }
        }

        private void OnRetry<T>(DelegateResult<T> delegateResult, TimeSpan time, int retryCount, Context context)
        {
            var retryContext = GetRetryContext(context);
            var ex = delegateResult.Exception;

            retryContext.LastException = ex;
            retryContext.RetryCount = retryCount;

            if (ex != null)
            {
                CallListenerOnError(retryContext, ex);
            }
        }

        private bool CallListenerOpen(RetryContext context)
        {
            var running = true;
            foreach (var listener in listeners)
            {
                running &= listener.Open(context);
            }

            return running;
        }

        private void CallListenerClose(RetryContext context, Exception ex)
        {
            context.SetAttribute(CLOSED, true);
            foreach (var listener in listeners)
            {
                listener.Close(context, ex);
            }
        }

        private void CallListenerOnError(RetryContext context, Exception ex)
        {
            foreach (var listener in listeners)
            {
                listener.OnError(context, ex);
            }
        }
    }
}