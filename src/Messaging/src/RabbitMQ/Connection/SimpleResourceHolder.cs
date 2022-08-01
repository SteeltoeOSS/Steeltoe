// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public static class SimpleResourceHolder
{
    private const string ForKey = "] for key [";

    private const string BoundToThread = "] bound to thread [";

    private static readonly AsyncLocal<Dictionary<object, object>> Resources = new ();

    private static readonly AsyncLocal<Dictionary<object, Stack<object>>> Stack = new ();

    private static readonly Dictionary<object, object> Empty = new ();

    public static IDictionary<object, object> GetResources()
    {
        var map = Resources.Value;
        if (map != null)
        {
            return new ReadOnlyDictionary<object, object>(map);
        }

        return Empty;
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
            logger?.LogTrace("Retrieved value [{value}]" + ForKey + "{key}" + BoundToThread + "{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
        }

        return value;
    }

    public static void Bind(object key, object value, ILogger logger = null)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var map = Resources.Value;

        // set ThreadLocal Map if none found
        if (map == null)
        {
            map = new Dictionary<object, object>();
            Resources.Value = map;
        }

        map.TryGetValue(key, out var oldValue);
        map[key] = value;
        if (oldValue != null)
        {
            throw new InvalidOperationException($"Already value [{oldValue}{ForKey}{key}{BoundToThread}{Thread.CurrentThread.ManagedThreadId}]");
        }

        logger?.LogTrace("Bound value [{value}" + ForKey + "{key}] to thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
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
            var dict = Stack.Value;
            if (dict == null)
            {
                dict = new Dictionary<object, Stack<object>>();
                Stack.Value = dict;
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
        var dict = Stack.Value;
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
                    Stack.Value = null;
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
            throw new InvalidOperationException($"No value for key [{key}{BoundToThread}{Thread.CurrentThread.ManagedThreadId}]");
        }

        return value;
    }

    public static object UnbindIfPossible(object key, ILogger logger = null)
    {
        var map = Resources.Value;
        if (map == null)
        {
            return null;
        }

        map.Remove(key, out var value);

        // Remove entire ThreadLocal if empty...
        if (map.Count == 0)
        {
            Resources.Value = null;
        }

        if (value != null)
        {
            logger?.LogTrace("Removed value [{value}" + ForKey + "{key}] from thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
        }

        return value;
    }

    public static void Clear()
    {
        Resources.Value = null;
        Stack.Value = null;
    }

    private static object DoGet(object actualKey, ILogger logger = null)
    {
        var map = Resources.Value;
        if (map == null)
        {
            return null;
        }

        map.TryGetValue(actualKey, out var result);
        return result;
    }
}
