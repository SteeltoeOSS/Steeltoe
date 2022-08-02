// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Messaging.RabbitMQ.Util;

public class ActiveObjectCounter<T>
{
    private readonly ConcurrentDictionary<T, CountdownEvent> _locks = new();

    public int Count => _locks.Count;

    public bool IsActive { get; private set; } = true;

    public void Add(T activeObject)
    {
        var latch = new CountdownEvent(1);
        _locks.TryAdd(activeObject, latch);
    }

    public void Release(T activeObject)
    {
        if (_locks.TryRemove(activeObject, out CountdownEvent remove))
        {
            remove.Signal();
        }
    }

    public bool Wait(TimeSpan timeout)
    {
        long t0 = DateTimeOffset.Now.Ticks;
        long t1 = t0 + timeout.Ticks;

        while (DateTimeOffset.Now.Ticks <= t1)
        {
            if (_locks.Count == 0)
            {
                return true;
            }

            var objects = new HashSet<T>(_locks.Keys);

            foreach (T activeObject in objects)
            {
                if (!_locks.TryGetValue(activeObject, out CountdownEvent latch))
                {
                    continue;
                }

                t0 = DateTimeOffset.Now.Ticks;

                if (t0 >= t1)
                {
                    break;
                }

                if (latch.Wait(TimeSpan.FromTicks(t1 - t0)))
                {
                    _locks.TryRemove(activeObject, out _);
                }
            }
        }

        return false;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Reset()
    {
        _locks.Clear();
        IsActive = false;
    }
}
