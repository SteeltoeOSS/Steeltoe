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
using Steeltoe.Common.Order;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Steeltoe.Common.Transaction
{
    // TODO: Move this to common
    public static class TransactionSynchronizationManager
    {
        private static readonly AsyncLocal<Dictionary<object, object>> _resources = new AsyncLocal<Dictionary<object, object>>();
        private static readonly AsyncLocal<ISet<ITransactionSynchronization>> _synchronizations = new AsyncLocal<ISet<ITransactionSynchronization>>();
        private static readonly AsyncLocal<bool> _actualTransactionActive = new AsyncLocal<bool>();
        private static readonly AsyncLocal<int?> _currentTransactionIsolationLevel = new AsyncLocal<int?>();
        private static readonly AsyncLocal<string> _currentTransactionName = new AsyncLocal<string>();
        private static readonly AsyncLocal<bool> _currentTransactionReadOnly = new AsyncLocal<bool>();

        private static readonly IDictionary<object, object> _emptyDict = new Dictionary<object, object>();
        private static readonly List<ITransactionSynchronization> _emptyList = new List<ITransactionSynchronization>();

        public static IDictionary<object, object> GetResourceMap()
        {
            var resources = _resources.Value;
            return (resources != null) ? new ReadOnlyDictionary<object, object>(resources) : _emptyDict;
        }

        public static bool HasResource(object key)
        {
            var value = DoGetResource(key);
            return value != null;
        }

        public static object GetResource(object key, ILogger logger = null)
        {
            var value = DoGetResource(key);
            if (value != null)
            {
                logger?.LogTrace("Retrieved value [" + value + "] for key [" + key + "] bound to thread [" + Thread.CurrentThread.ManagedThreadId + "]");
            }

            return value;
        }

        public static void BindResource(object key, object value, ILogger logger = null)
        {
            if (value == null)
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

            // Transparently suppress a ResourceHolder that was marked as void...
            if (oldValue is IResourceHolder && ((IResourceHolder)oldValue).IsVoid)
            {
                oldValue = null;
            }

            if (oldValue != null)
            {
                throw new InvalidOperationException("Already value [" + oldValue + "] for key [" + key + "] bound to thread [" + Thread.CurrentThread.ManagedThreadId + "]");
            }

            logger?.LogTrace("Bound value [" + value + "] for key [" + key + "] to thread [" + Thread.CurrentThread.ManagedThreadId + "]");
        }

        public static object UnbindResource(object key, ILogger logger = null)
        {
            var value = DoUnbindResource(key, logger);
            if (value == null)
            {
                throw new InvalidOperationException(
                        "No value for key [" + key + "] bound to thread [" + Thread.CurrentThread.ManagedThreadId + "]");
            }

            return value;
        }

        public static object UnbindResourceIfPossible(object key, ILogger logger = null)
        {
            return DoUnbindResource(key, logger);
        }

        public static bool IsSynchronizationActive()
        {
            return _synchronizations.Value != null;
        }

        public static void InitSynchronization(ILogger logger = null)
        {
            if (IsSynchronizationActive())
            {
                throw new InvalidOperationException("Cannot activate transaction synchronization - already active");
            }

            logger?.LogTrace("Initializing transaction synchronization");
            _synchronizations.Value = new HashSet<ITransactionSynchronization>();
        }

        public static void RegisterSynchronization(ITransactionSynchronization synchronization)
        {
            if (synchronization == null)
            {
                throw new ArgumentNullException(nameof(synchronization));
            }

            var synchs = _synchronizations.Value;
            if (synchs == null)
            {
                throw new InvalidOperationException("Transaction synchronization is not active");
            }

            synchs.Add(synchronization);
        }

        public static List<ITransactionSynchronization> GetSynchronizations()
        {
            var synchs = _synchronizations.Value;
            if (synchs == null)
            {
                throw new InvalidOperationException("Transaction synchronization is not active");
            }

            // Return unmodifiable snapshot, to avoid ConcurrentModificationExceptions
            // while iterating and invoking synchronization callbacks that in turn
            // might register further synchronizations.
            if (synchs.Count == 0)
            {
                return _emptyList;
            }
            else
            {
                // Sort lazily here, not in registerSynchronization.
                var sortedOrdered = new List<IOrdered>();
                var unordered = new List<ITransactionSynchronization>();
                foreach (var s in synchs)
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
                unordered.InsertRange(0, sortedOrdered.Select((o) => o as ITransactionSynchronization));

                // AnnotationAwareOrderComparator.sort(sortedSynchs);
                return unordered;
            }
        }

        public static void ClearSynchronization(ILogger logger = null)
        {
            if (!IsSynchronizationActive())
            {
                throw new InvalidOperationException("Cannot deactivate transaction synchronization - not active");
            }

            logger?.LogTrace("Clearing transaction synchronization");
            _synchronizations.Value = null;
        }

        public static bool IsActualTransactionActive()
        {
            return _actualTransactionActive.Value;
        }

        public static void SetActualTransactionActive(bool active)
        {
            _actualTransactionActive.Value = active;
        }

        public static int? GetCurrentTransactionIsolationLevel()
        {
            return _currentTransactionIsolationLevel.Value;
        }

        public static void SetCurrentTransactionIsolationLevel(int? isolationLevel)
        {
            _currentTransactionIsolationLevel.Value = isolationLevel;
        }

        public static void SetCurrentTransactionName(string name)
        {
            _currentTransactionName.Value = name;
        }

        public static string GetCurrentTransactionName()
        {
            return _currentTransactionName.Value;
        }

        public static void SetCurrentTransactionReadOnly(bool readOnly)
        {
            _currentTransactionReadOnly.Value = readOnly;
        }

        public static bool IsCurrentTransactionReadOnly()
        {
            return _currentTransactionReadOnly.Value;
        }

        public static void Clear()
        {
            _synchronizations.Value = null;
            _currentTransactionName.Value = null;
            _currentTransactionReadOnly.Value = false;
            _currentTransactionIsolationLevel.Value = null;
            _actualTransactionActive.Value = false;
        }

        private static object DoUnbindResource(object actualKey, ILogger logger = null)
        {
            var map = _resources.Value;
            if (map == null)
            {
                return null;
            }

            map.TryGetValue(actualKey, out var value);
            map.Remove(actualKey);

            // Remove entire ThreadLocal if empty...
            if (map.Count == 0)
            {
                _resources.Value = null;
            }

            // Transparently suppress a ResourceHolder that was marked as void...
            if (value is IResourceHolder && ((IResourceHolder)value).IsVoid)
            {
                value = null;
            }

            logger?.LogTrace("Removed value [" + value + "] for key [" + actualKey + "] from thread [" + Thread.CurrentThread.ManagedThreadId + "]");
            return value;
        }

        private static object DoGetResource(object actualKey)
        {
            var map = _resources.Value;
            if (map == null)
            {
                return null;
            }

            map.TryGetValue(actualKey, out var value);

            // Transparently remove ResourceHolder that was marked as void...
            if (value is IResourceHolder && ((IResourceHolder)value).IsVoid)
            {
                map.Remove(actualKey);

                // Remove entire ThreadLocal if empty...
                if (map.Count == 0)
                {
                    _resources.Value = null;
                }

                value = null;
            }

            return value;
        }
    }
}
