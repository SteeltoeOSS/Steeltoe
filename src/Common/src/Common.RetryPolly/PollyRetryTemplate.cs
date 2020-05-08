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

using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Retry
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
            return Execute<T>(retryCallback, (IRecoveryCallback<T>)null);
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback, Func<IRetryContext, T> recoveryCallback)
        {
            var recovCallback = new FuncRecoveryCallback<T>(recoveryCallback);
            return Execute<T>(retryCallback, recovCallback);
        }

        public override void Execute(Action<IRetryContext> retryCallback, Action<IRetryContext> recoveryCallback)
        {
            var recovCallback = new ActionRecoveryCallback(recoveryCallback);
            Execute(retryCallback, recovCallback);
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback)
        {
            var policy = BuildPolicy<T>();
            var retryContext = new RetryContext();
            var context = new Context
            {
                { RETRYCONTEXT_KEY, retryContext }
            };
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
            Execute(retryCallback, (IRecoveryCallback)null);
        }

        public override void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback)
        {
            var policy = BuildPolicy<object>();
            var retryContext = new RetryContext();
            var context = new Context
            {
                { RETRYCONTEXT_KEY, retryContext }
            };
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
                            var result = default(T);
                            if (retryContext.GetAttribute(RECOVERY_CALLBACK_KEY) is IRecoveryCallback callback)
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

        private class FuncRecoveryCallback<T> : IRecoveryCallback<T>
        {
            private readonly Func<IRetryContext, T> _func;

            public FuncRecoveryCallback(Func<IRetryContext, T> func)
            {
                _func = func;
            }

            public T Recover(IRetryContext context)
            {
                return _func(context);
            }

            object IRecoveryCallback.Recover(IRetryContext context)
            {
                return _func(context);
            }
        }

        private class ActionRecoveryCallback : IRecoveryCallback
        {
            private readonly Action<IRetryContext> _action;

            public ActionRecoveryCallback(Action<IRetryContext> action)
            {
                _action = action;
            }

            public object Recover(IRetryContext context)
            {
                _action(context);
                return null;
            }
        }
    }
}