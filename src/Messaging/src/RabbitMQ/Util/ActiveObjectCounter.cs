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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Util
{
    public class ActiveObjectCounter<T>
    {
        private readonly ConcurrentDictionary<T, CountdownEvent> _locks = new ConcurrentDictionary<T, CountdownEvent>();

        public void Add(T activeObject)
        {
            var latch = new CountdownEvent(1);
            _locks.TryAdd(activeObject, latch);
        }

        public void Release(T activeObject)
        {
            if (_locks.TryRemove(activeObject, out var remove))
            {
                remove.Signal();
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            var t0 = DateTimeOffset.Now.Ticks;
            var t1 = t0 + timeout.Ticks;
            while (DateTimeOffset.Now.Ticks <= t1)
            {
                if (_locks.Count == 0)
                {
                    return true;
                }

                var objects = new HashSet<T>(_locks.Keys);
                foreach (var activeObject in objects)
                {
                    if (!_locks.TryGetValue(activeObject, out var latch))
                    {
                        continue;
                    }

                    t0 = DateTimeOffset.Now.Ticks;

                    if (latch.Wait(TimeSpan.FromTicks(t1 - t0)))
                    {
                        _locks.TryRemove(activeObject, out _);
                    }
                }
            }

            return false;
        }

        public int Count => _locks.Count;

        public bool IsActive { get; private set; }

        public void Deactivate() => IsActive = false;

        public void Reset()
        {
            _locks.Clear();
            IsActive = false;
        }
    }
}
