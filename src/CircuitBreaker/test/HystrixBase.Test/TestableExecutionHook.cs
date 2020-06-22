﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class TestableExecutionHook : HystrixCommandExecutionHook
    {
        public ITestOutputHelper _output;

        public TestableExecutionHook()
        {
        }

        public TestableExecutionHook(ITestOutputHelper output)
        {
            _output = output;
        }

        private static void RecordHookCall(StringBuilder sequenceRecorder, string methodName)
        {
            sequenceRecorder.Append(methodName).Append(" - ");
        }

        internal StringBuilder ExecutionSequence = new StringBuilder();
        internal List<Notification<object>> CommandEmissions = new List<Notification<object>>();
        internal List<Notification<object>> ExecutionEmissions = new List<Notification<object>>();
        internal List<Notification<object>> FallbackEmissions = new List<Notification<object>>();

        public bool CommandEmissionsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(CommandEmissions, numOnNext, numOnError, numOnCompleted);
        }

        public bool ExecutionEventsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(ExecutionEmissions, numOnNext, numOnError, numOnCompleted);
        }

        public bool FallbackEventsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(FallbackEmissions, numOnNext, numOnError, numOnCompleted);
        }

        public Exception GetCommandException()
        {
            return GetException(CommandEmissions);
        }

        public Exception GetExecutionException()
        {
            return GetException(ExecutionEmissions);
        }

        public Exception GetFallbackException()
        {
            return GetException(FallbackEmissions);
        }

        public override void OnStart(IHystrixInvokable commandInstance)
        {
            base.OnStart(commandInstance);
            RecordHookCall(ExecutionSequence, "onStart");
        }

        public override T OnEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            CommandEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(ExecutionSequence, "onEmit");
            return base.OnEmit(commandInstance, value);
        }

        public override Exception OnError(IHystrixInvokable commandInstance, FailureType failureType, Exception e)
        {
            CommandEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(ExecutionSequence, "onError");
            return base.OnError(commandInstance, failureType, e);
        }

        public override void OnSuccess(IHystrixInvokable commandInstance)
        {
            CommandEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(ExecutionSequence, "onSuccess");
            base.OnSuccess(commandInstance);
        }

        public override void OnThreadStart(IHystrixInvokable commandInstance)
        {
            base.OnThreadStart(commandInstance);
            RecordHookCall(ExecutionSequence, "onThreadStart");
        }

        public override void OnThreadComplete(IHystrixInvokable commandInstance)
        {
            base.OnThreadComplete(commandInstance);
            RecordHookCall(ExecutionSequence, "onThreadComplete");
        }

        public override void OnExecutionStart(IHystrixInvokable commandInstance)
        {
            RecordHookCall(ExecutionSequence, "onExecutionStart");
            base.OnExecutionStart(commandInstance);
        }

        public override T OnExecutionEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            ExecutionEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(ExecutionSequence, "onExecutionEmit");
            return base.OnExecutionEmit(commandInstance, value);
        }

        public override Exception OnExecutionError(IHystrixInvokable commandInstance, Exception e)
        {
            ExecutionEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(ExecutionSequence, "onExecutionError");
            return base.OnExecutionError(commandInstance, e);
        }

        public override void OnExecutionSuccess(IHystrixInvokable commandInstance)
        {
            ExecutionEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(ExecutionSequence, "onExecutionSuccess");
            base.OnExecutionSuccess(commandInstance);
        }

        public override void OnFallbackStart(IHystrixInvokable commandInstance)
        {
            base.OnFallbackStart(commandInstance);
            RecordHookCall(ExecutionSequence, "onFallbackStart");
        }

        public override T OnFallbackEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            FallbackEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(ExecutionSequence, "onFallbackEmit");
            return base.OnFallbackEmit(commandInstance, value);
        }

        public override Exception OnFallbackError(IHystrixInvokable commandInstance, Exception e)
        {
            FallbackEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(ExecutionSequence, "onFallbackError");
            return base.OnFallbackError(commandInstance, e);
        }

        public override void OnFallbackSuccess(IHystrixInvokable commandInstance)
        {
            FallbackEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(ExecutionSequence, "onFallbackSuccess");
            base.OnFallbackSuccess(commandInstance);
        }

        public override void OnCacheHit(IHystrixInvokable commandInstance)
        {
            base.OnCacheHit(commandInstance);
            RecordHookCall(ExecutionSequence, "onCacheHit");
        }

        private bool EventsMatch(List<Notification<object>> l, int numOnNext, int numOnError, int numOnCompleted)
        {
            bool matchFailed = false;
            int actualOnNext = 0;
            int actualOnError = 0;
            int actualOnCompleted = 0;

            if (l.Count != numOnNext + numOnError + numOnCompleted)
            {
                _output?.WriteLine("Actual : " + l + ", Expected : " + numOnNext + " OnNexts, " + numOnError + " OnErrors, " + numOnCompleted + " OnCompleted");
                return false;
            }

            for (int n = 0; n < numOnNext; n++)
            {
                Notification<object> current = l[n];
                if (current.Kind != NotificationKind.OnNext)
                {
                    matchFailed = true;
                }
                else
                {
                    actualOnNext++;
                }
            }

            for (int e = numOnNext; e < numOnNext + numOnError; e++)
            {
                Notification<object> current = l[e];
                if (current.Kind != NotificationKind.OnError)
                {
                    matchFailed = true;
                }
                else
                {
                    actualOnError++;
                }
            }

            for (int c = numOnNext + numOnError; c < numOnNext + numOnError + numOnCompleted; c++)
            {
                Notification<object> current = l[c];
                if (current.Kind != NotificationKind.OnCompleted)
                {
                    matchFailed = true;
                }
                else
                {
                    actualOnCompleted++;
                }
            }

            if (matchFailed)
            {
                _output?.WriteLine("Expected : " + numOnNext + " OnNexts, " + numOnError + " OnErrors, and " + numOnCompleted);
                _output?.WriteLine("Actual : " + actualOnNext + " OnNexts, " + actualOnError + " OnErrors, and " + actualOnCompleted);
            }

            return !matchFailed;
        }

        private Exception GetException(List<Notification<object>> l)
        {
            foreach (Notification<object> n in l)
            {
                if (n.Kind == NotificationKind.OnError)
                {
                    _output?.WriteLine(n.Exception.ToString());
                    return n.Exception;
                }
            }

            return null;
        }
    }
}
