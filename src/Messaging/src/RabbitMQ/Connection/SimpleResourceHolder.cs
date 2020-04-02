﻿// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public static class SimpleResourceHolder
    {
        private const string FOR_KEY = "] for key [";

        private const string BOUND_TO_THREAD = "] bound to thread [";

        private static readonly AsyncLocal<Dictionary<object, object>> _resources = new AsyncLocal<Dictionary<object, object>>();

        private static readonly AsyncLocal<Dictionary<object, Stack<object>>> _stack = new AsyncLocal<Dictionary<object, Stack<object>>>();

        private static Dictionary<object, object> _empty = new Dictionary<object, object>();

        public static IDictionary<object, object> GetResources()
        {
            var map = _resources.Value;
            if (map != null)
            {
                return new ReadOnlyDictionary<object, object>(map);
            }

            return _empty;
        }

        public static bool Has(object key, ILogger logger = null)
        {
            var value = DoGet(key, logger);
            return value != null;
        }

        public static object Get(object key, ILogger logger = null)
        {
            var value = DoGet(key);
            if (value != null)
            {
                logger?.LogTrace("Retrieved value [" + value + FOR_KEY + key + BOUND_TO_THREAD + Thread.CurrentThread.ManagedThreadId + "]");
            }

            return value;
        }

        public static void Bind(object key, object value, ILogger logger = null)
        {
            if (value != null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var map = _resources.Value;

            // set ThreadLocal Map if none found
            if (map == null)
            {
                map = new Dictionary<object, object>();
                _resources.Value = map;
            }

            map.TryGetValue(key, out var oldValue);
            map[key] = value;
            if (oldValue != null)
            {
                throw new InvalidOperationException("Already value [" + oldValue + FOR_KEY + key + BOUND_TO_THREAD + Thread.CurrentThread.ManagedThreadId + "]");
            }

            logger?.LogTrace("Bound value [" + value + FOR_KEY + key + "] to thread [" + Thread.CurrentThread.ManagedThreadId + "]");
        }

        public static void Push(object key, object value, ILogger logger = null)
        {
            var currentValue = Get(key);
            if (currentValue == null)
            {
                Bind(key, value);
            }
            else
            {
                var dict = _stack.Value;
                if (dict == null)
                {
                    dict = new Dictionary<object, Stack<object>>();
                    _stack.Value = dict;
                }

                if (!dict.TryGetValue(key, out var stack))
                {
                    stack = new Stack<object>();
                    dict.Add(key, stack);
                }

                stack.Push(currentValue);
                Unbind(key);
                Bind(key, value);
            }
        }

        public static object Pop(object key, ILogger logger = null)
        {
            var popped = Unbind(key);
            var dict = _stack.Value;
            if (dict != null)
            {
                dict.TryGetValue(key, out var deque);
                if (deque != null && deque.Count > 0)
                {
                    var previousValue = deque.Pop();
                    if (previousValue != null)
                    {
                        Bind(key, previousValue);
                    }

                    if (deque.Count == 0)
                    {
                        _stack.Value = null;
                    }
                }
            }

            return popped;
        }

        public static object Unbind(object key, ILogger logger = null)
        {
            var value = UnbindIfPossible(key);
            if (value == null)
            {
                throw new InvalidOperationException("No value for key [" + key + BOUND_TO_THREAD + Thread.CurrentThread.ManagedThreadId + "]");
            }

            return value;
        }

        public static object UnbindIfPossible(object key, ILogger logger = null)
        {
            var map = _resources.Value;
            if (map == null)
            {
                return null;
            }

            map.Remove(key, out var value);

            // Remove entire ThreadLocal if empty...
            if (map.Count == 0)
            {
                _resources.Value = null;
            }

            if (value != null)
            {
                logger?.LogTrace("Removed value [" + value + FOR_KEY + key + "] from thread [" + Thread.CurrentThread.ManagedThreadId + "]");
            }

            return value;
        }

        public static void Clear()
        {
            _resources.Value = null;
            _stack.Value = null;
        }

        private static object DoGet(object actualKey, ILogger logger = null)
        {
            var map = _resources.Value;
            if (map == null)
            {
                return null;
            }

            map.TryGetValue(actualKey, out var result);
            return result;
        }
    }
}
