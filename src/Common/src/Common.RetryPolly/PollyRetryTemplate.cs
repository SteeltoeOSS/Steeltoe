// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;
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
        private readonly ILogger _logger;

        public PollyRetryTemplate(int maxAttempts, int backOffInitialInterval, int backOffMaxInterval, double backOffMultiplier, ILogger logger = null)
            : this(new Dictionary<Type, bool>(), maxAttempts, true, backOffInitialInterval, backOffMaxInterval, backOffMultiplier, logger)
        {
        }

        public PollyRetryTemplate(Dictionary<Type, bool> retryableExceptions, int maxAttempts, bool defaultRetryable, int backOffInitialInterval, int backOffMaxInterval, double backOffMultiplier, ILogger logger = null)
        {
            _retryableExceptions = new BinaryExceptionClassifier(retryableExceptions, defaultRetryable);
            _maxAttempts = maxAttempts;
            _backOffInitialInterval = backOffInitialInterval;
            _backOffMaxInterval = backOffMaxInterval;
            _backOffMultiplier = backOffMultiplier;
            _logger = logger;
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback)
        {
            return Execute<T>(retryCallback, (IRecoveryCallback<T>)null);
        }

        public override T Execute<T>(Func<IRetryContext, T> retryCallback, Func<IRetryContext, T> recoveryCallback)
        {
            var recovCallback = new FuncRecoveryCallback<T>(recoveryCallback, _logger);
            return Execute<T>(retryCallback, recovCallback);
        }

        public override void Execute(Action<IRetryContext> retryCallback, Action<IRetryContext> recoveryCallback)
        {
            var recovCallback = new ActionRecoveryCallback(recoveryCallback, _logger);
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
            RetrySynchronizationManager.Register(retryContext);
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
                        var recovered = (bool?)retryContext.GetAttribute(RECOVERED);
                        if (recovered != null && recovered.Value)
                        {
                            callbackResult = (T)retryContext.GetAttribute(RECOVERED_RESULT);
                        }
                    }

                    return callbackResult;
                }, context);

            CallListenerClose(retryContext, retryContext.LastException);
            RetrySynchronizationManager.Clear();
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
            RetrySynchronizationManager.Register(retryContext);
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
            RetrySynchronizationManager.Clear();
        }

        private Policy<T> BuildPolicy<T>()
        {
            var delay = Backoff.ExponentialBackoff(TimeSpan.FromMilliseconds(_backOffInitialInterval), _maxAttempts - 1, _backOffMultiplier, true);
            var retryPolicy = Policy<T>.HandleInner<Exception>((e) => _retryableExceptions.Classify(e))
            .WaitAndRetry(delay, OnRetry);

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
                            else if (delegateResult.Exception != null)
                            {
                                throw delegateResult.Exception;
                            }

                            return result;
                        }, (ex, context) =>
                        {
                            _logger?.LogError(ex.Exception, $"Context: {context}");

                            // throw ex.Exception; throwing here doesn't allow the fall back to work.
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
                var result = new RetryContext();
                RetrySynchronizationManager.Register(result);
                return result;
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
            private readonly ILogger _logger;

            public FuncRecoveryCallback(Func<IRetryContext, T> func, ILogger logger)
            {
                _func = func;
                _logger = logger;
            }

            public T Recover(IRetryContext context)
            {
                _logger?.LogTrace($"FuncRecovery Context: {context}");
                return _func(context);
            }

            object IRecoveryCallback.Recover(IRetryContext context)
            {
                _logger?.LogTrace($"FuncRecovery Context: {context}");
                return _func(context);
            }
        }

        private class ActionRecoveryCallback : IRecoveryCallback
        {
            private readonly Action<IRetryContext> _action;
            private readonly ILogger _logger;

            public ActionRecoveryCallback(Action<IRetryContext> action, ILogger logger)
            {
                _action = action;
                _logger = logger;
            }

            public object Recover(IRetryContext context)
            {
                _logger?.LogTrace($"ActionRecovery Context: {context}");
                _action(context);
                return null;
            }
        }
    }
}