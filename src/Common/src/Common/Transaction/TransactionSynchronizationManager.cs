// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Order;

namespace Steeltoe.Common.Transaction;

public static class TransactionSynchronizationManager
{
    private static readonly AsyncLocal<Dictionary<object, object>> Resources = new();
    private static readonly AsyncLocal<ISet<ITransactionSynchronization>> Synchronizations = new();
    private static readonly AsyncLocal<bool> ActualTransactionActive = new();
    private static readonly AsyncLocal<int?> CurrentTransactionIsolationLevel = new();
    private static readonly AsyncLocal<string> CurrentTransactionName = new();
    private static readonly AsyncLocal<bool> CurrentTransactionReadOnly = new();

    private static readonly IDictionary<object, object> EmptyDict = new Dictionary<object, object>();
    private static readonly List<ITransactionSynchronization> EmptyList = new();

    public static IDictionary<object, object> GetResourceMap()
    {
        Dictionary<object, object> resources = Resources.Value;
        return resources != null ? new ReadOnlyDictionary<object, object>(resources) : EmptyDict;
    }

    public static bool HasResource(object key)
    {
        object value = DoGetResource(key);
        return value != null;
    }

    public static object GetResource(object key, ILogger logger = null)
    {
        object value = DoGetResource(key);

        if (value != null)
        {
            logger?.LogTrace("Retrieved value [{value}] for key [{key}] bound to thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
        }

        return value;
    }

    public static void BindResource(object key, object value, ILogger logger = null)
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

        // Transparently suppress a ResourceHolder that was marked as void...
        if (oldValue is IResourceHolder holder && holder.IsVoid)
        {
            oldValue = null;
        }

        if (oldValue != null)
        {
            throw new InvalidOperationException($"Already value [{oldValue}] for key [{key}] bound to thread [{Thread.CurrentThread.ManagedThreadId}]");
        }

        logger?.LogTrace("Bound value [{value}] for key [{key}] to thread [{thread}]", value, key, Thread.CurrentThread.ManagedThreadId);
    }

    public static object UnbindResource(object key, ILogger logger = null)
    {
        object value = DoUnbindResource(key, logger);

        if (value == null)
        {
            throw new InvalidOperationException($"No value for key [{key}] bound to thread [{Thread.CurrentThread.ManagedThreadId}]");
        }

        return value;
    }

    public static object UnbindResourceIfPossible(object key, ILogger logger = null)
    {
        return DoUnbindResource(key, logger);
    }

    public static bool IsSynchronizationActive()
    {
        return Synchronizations.Value != null;
    }

    public static void InitSynchronization(ILogger logger = null)
    {
        if (IsSynchronizationActive())
        {
            throw new InvalidOperationException("Cannot activate transaction synchronization - already active");
        }

        logger?.LogTrace("Initializing transaction synchronization");
        Synchronizations.Value = new HashSet<ITransactionSynchronization>();
    }

    public static void RegisterSynchronization(ITransactionSynchronization synchronization)
    {
        ArgumentGuard.NotNull(synchronization);

        ISet<ITransactionSynchronization> synchs = Synchronizations.Value;

        if (synchs == null)
        {
            throw new InvalidOperationException("Transaction synchronization is not active");
        }

        synchs.Add(synchronization);
    }

    public static List<ITransactionSynchronization> GetSynchronizations()
    {
        ISet<ITransactionSynchronization> synchs = Synchronizations.Value;

        if (synchs == null)
        {
            throw new InvalidOperationException("Transaction synchronization is not active");
        }

        // Return unmodifiable snapshot, to avoid ConcurrentModificationExceptions
        // while iterating and invoking synchronization callbacks that in turn
        // might register further synchronizations.
        if (synchs.Count == 0)
        {
            return EmptyList;
        }

        // Sort lazily here, not in registerSynchronization.
        var sortedOrdered = new List<IOrdered>();
        var unordered = new List<ITransactionSynchronization>();

        foreach (ITransactionSynchronization s in synchs)
        {
            if (s is IOrdered)
            {
                var ordered = s as IOrdered;
                sortedOrdered.Add(ordered);
            }
            else
            {
                unordered.Add(s);
            }
        }

        sortedOrdered.Sort(OrderComparer.Instance);
        unordered.InsertRange(0, sortedOrdered.Select(o => o as ITransactionSynchronization));

        // AnnotationAwareOrderComparator.sort(sortedSynchs);
        return unordered;
    }

    public static void ClearSynchronization(ILogger logger = null)
    {
        if (!IsSynchronizationActive())
        {
            throw new InvalidOperationException("Cannot deactivate transaction synchronization - not active");
        }

        logger?.LogTrace("Clearing transaction synchronization");
        Synchronizations.Value = null;
    }

    public static bool IsActualTransactionActive()
    {
        return ActualTransactionActive.Value;
    }

    public static void SetActualTransactionActive(bool active)
    {
        ActualTransactionActive.Value = active;
    }

    public static int? GetCurrentTransactionIsolationLevel()
    {
        return CurrentTransactionIsolationLevel.Value;
    }

    public static void SetCurrentTransactionIsolationLevel(int? isolationLevel)
    {
        CurrentTransactionIsolationLevel.Value = isolationLevel;
    }

    public static void SetCurrentTransactionName(string name)
    {
        CurrentTransactionName.Value = name;
    }

    public static string GetCurrentTransactionName()
    {
        return CurrentTransactionName.Value;
    }

    public static void SetCurrentTransactionReadOnly(bool readOnly)
    {
        CurrentTransactionReadOnly.Value = readOnly;
    }

    public static bool IsCurrentTransactionReadOnly()
    {
        return CurrentTransactionReadOnly.Value;
    }

    public static void Clear()
    {
        Synchronizations.Value = null;
        CurrentTransactionName.Value = null;
        CurrentTransactionReadOnly.Value = false;
        CurrentTransactionIsolationLevel.Value = null;
        ActualTransactionActive.Value = false;
    }

    private static object DoUnbindResource(object actualKey, ILogger logger = null)
    {
        Dictionary<object, object> map = Resources.Value;

        if (map == null)
        {
            return null;
        }

        map.TryGetValue(actualKey, out object value);
        map.Remove(actualKey);

        // Remove entire ThreadLocal if empty...
        if (map.Count == 0)
        {
            Resources.Value = null;
        }

        // Transparently suppress a ResourceHolder that was marked as void...
        if (value is IResourceHolder holder && holder.IsVoid)
        {
            value = null;
        }

        logger?.LogTrace("Removed value [{value}] for key [{key}] from thread [{thread}]", value, actualKey, Thread.CurrentThread.ManagedThreadId);
        return value;
    }

    private static object DoGetResource(object actualKey)
    {
        Dictionary<object, object> map = Resources.Value;

        if (map == null)
        {
            return null;
        }

        map.TryGetValue(actualKey, out object value);

        // Transparently remove ResourceHolder that was marked as void...
        if (value is IResourceHolder holder && holder.IsVoid)
        {
            map.Remove(actualKey);

            // Remove entire ThreadLocal if empty...
            if (map.Count == 0)
            {
                Resources.Value = null;
            }

            value = null;
        }

        return value;
    }
}
