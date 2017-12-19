//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        ITestOutputHelper _output;
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

        internal StringBuilder executionSequence = new StringBuilder();
        List<Notification<object>> commandEmissions = new List<Notification<object>>();
        List<Notification<object>> executionEmissions = new List<Notification<object>>();
        List<Notification<object>> fallbackEmissions = new List<Notification<object>>();

        public bool CommandEmissionsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(commandEmissions, numOnNext, numOnError, numOnCompleted);
        }

        public bool ExecutionEventsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(executionEmissions, numOnNext, numOnError, numOnCompleted);
        }

        public bool FallbackEventsMatch(int numOnNext, int numOnError, int numOnCompleted)
        {
            return EventsMatch(fallbackEmissions, numOnNext, numOnError, numOnCompleted);
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
                Notification <object> current = l[n];
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
                Notification <object> current = l[e];
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
                Notification <object> current = l[c];
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

        public Exception GetCommandException()
        {
            return GetException(commandEmissions);
        }

        public Exception GetExecutionException()
        {
            return GetException(executionEmissions);
        }

        public Exception GetFallbackException()
        {
            return GetException(fallbackEmissions);
        }

        private Exception GetException(List<Notification<object>> l)
        {
            foreach (Notification <object> n in l)
            {
                if (n.Kind == NotificationKind.OnError)
                {

                    _output?.WriteLine(n.Exception.ToString());
                    return n.Exception;
                }
            }
            return null;
        }

        public override void OnStart(IHystrixInvokable commandInstance)
        {
            base.OnStart(commandInstance);
            RecordHookCall(executionSequence, "onStart");
        }

        public override T OnEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            commandEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(executionSequence, "onEmit");
            return base.OnEmit(commandInstance, value);
        }

        public override Exception OnError(IHystrixInvokable commandInstance, FailureType failureType, Exception e)
        {
            commandEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(executionSequence, "onError");
            return base.OnError(commandInstance, failureType, e);
        }

        public override void OnSuccess(IHystrixInvokable commandInstance)
        {
            commandEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(executionSequence, "onSuccess");
            base.OnSuccess(commandInstance);
        }

        public override void OnThreadStart(IHystrixInvokable commandInstance)
        {
            base.OnThreadStart(commandInstance);
            RecordHookCall(executionSequence, "onThreadStart");
        }

        public override void OnThreadComplete(IHystrixInvokable commandInstance)
        {
            base.OnThreadComplete(commandInstance);
            RecordHookCall(executionSequence, "onThreadComplete");
        }

        public override void OnExecutionStart(IHystrixInvokable commandInstance)
        {
            RecordHookCall(executionSequence, "onExecutionStart");
            base.OnExecutionStart(commandInstance);
        }

        public override T OnExecutionEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            executionEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(executionSequence, "onExecutionEmit");
            return base.OnExecutionEmit(commandInstance, value);
        }

        public override Exception OnExecutionError(IHystrixInvokable commandInstance, Exception e)
        {
            executionEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(executionSequence, "onExecutionError");
            return base.OnExecutionError(commandInstance, e);
        }

        public override void OnExecutionSuccess(IHystrixInvokable commandInstance)
        {
            executionEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(executionSequence, "onExecutionSuccess");
            base.OnExecutionSuccess(commandInstance);
        }

        public override void OnFallbackStart(IHystrixInvokable commandInstance)
        {
            base.OnFallbackStart(commandInstance);
            RecordHookCall(executionSequence, "onFallbackStart");
        }

        public override T OnFallbackEmit<T>(IHystrixInvokable commandInstance, T value)
        {
            fallbackEmissions.Add(Notification.CreateOnNext<object>(value));
            RecordHookCall(executionSequence, "onFallbackEmit");
            return base.OnFallbackEmit(commandInstance, value);
        }

        public override Exception OnFallbackError(IHystrixInvokable commandInstance, Exception e)
        {
            fallbackEmissions.Add(Notification.CreateOnError<object>(e));
            RecordHookCall(executionSequence, "onFallbackError");
            return base.OnFallbackError(commandInstance, e);
        }

        public override void OnFallbackSuccess(IHystrixInvokable commandInstance)
        {
            fallbackEmissions.Add(Notification.CreateOnCompleted<object>());
            RecordHookCall(executionSequence, "onFallbackSuccess");
            base.OnFallbackSuccess(commandInstance);
        }

        public override void OnCacheHit(IHystrixInvokable commandInstance)
        {
            base.OnCacheHit(commandInstance);
            RecordHookCall(executionSequence, "onCacheHit");
        }

    }
}
