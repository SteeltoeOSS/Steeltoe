// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public static class SimpleResourceHolder
{
    private const string ForKey = "] for key [";

    private const string BoundToThread = "] bound to thread [";

    private static readonly AsyncLocal<Dictionary<object, object>> Resources = new();

    private static readonly AsyncLocal<Dictionary<object, Stack<object>>> Stack = new();

    private static readonly Dictionary<object, object> Empty = new();

    public static IDictionary<object, object> GetResources()
    {
        Dictionary<object, object> map = Resources.Value;

        if (map != null)
        {
            return new ReadOnlyDictionary<object, object>(map);
        }

        return Empty;
    }

    public static bool Has(object key, ILogger logger = null)
    {
        object value = DoGet(key);
        return value != null;
    }

    public static object Get(object key, ILogger logger = null)
    {
        object value = DoGet(key);

        if (value != null)
        {
            logger?.LogTrace("Retrieved value [{value}]] for key [{key}] bound to thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
        }

        return value;
    }

    public static void Bind(object key, object value, ILogger logger = null)
    {
        ArgumentGuard.NotNull(value);

        Dictionary<object, object> map = Resources.Value;

        // set ThreadLocal Map if none found
        if (map == null)
        {
            map = new Dictionary<object, object>();
            Resources.Value = map;
        }

        map.TryGetValue(key, out object oldValue);
        map[key] = value;

        if (oldValue != null)
        {
            throw new InvalidOperationException($"Already value [{oldValue}{ForKey}{key}{BoundToThread}{Thread.CurrentThread.ManagedThreadId}]");
        }

        logger?.LogTrace("Bound value [{value}] for key [{key}] to thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
    }

    public static void Push(object key, object value, ILogger logger = null)
    {
        object currentValue = Get(key);

        if (currentValue == null)
        {
            Bind(key, value);
        }
        else
        {
            Dictionary<object, Stack<object>> dict = Stack.Value;

            if (dict == null)
            {
                dict = new Dictionary<object, Stack<object>>();
                Stack.Value = dict;
            }

            if (!dict.TryGetValue(key, out Stack<object> stack))
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
        object popped = Unbind(key);
        Dictionary<object, Stack<object>> dict = Stack.Value;

        if (dict != null)
        {
            dict.TryGetValue(key, out Stack<object> deque);

            if (deque != null && deque.Count > 0)
            {
                object previousValue = deque.Pop();

                if (previousValue != null)
                {
                    Bind(key, previousValue);
                }

                if (deque.Count == 0)
                {
                    Stack.Value = null;
                }
            }
        }

        return popped;
    }

    public static object Unbind(object key, ILogger logger = null)
    {
        object value = UnbindIfPossible(key);

        if (value == null)
        {
            throw new InvalidOperationException($"No value for key [{key}{BoundToThread}{Thread.CurrentThread.ManagedThreadId}]");
        }

        return value;
    }

    public static object UnbindIfPossible(object key, ILogger logger = null)
    {
        Dictionary<object, object> map = Resources.Value;

        if (map == null)
        {
            return null;
        }

        map.Remove(key, out object value);

        // Remove entire ThreadLocal if empty...
        if (map.Count == 0)
        {
            Resources.Value = null;
        }

        if (value != null)
        {
            logger?.LogTrace("Removed value [{value}] for key [{key}] from thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
        }

        return value;
    }

    public static void Clear()
    {
        Resources.Value = null;
        Stack.Value = null;
    }

    private static object DoGet(object actualKey)
    {
        Dictionary<object, object> map = Resources.Value;

        if (map == null)
        {
            return null;
        }

        map.TryGetValue(actualKey, out object result);
        return result;
    }
}
