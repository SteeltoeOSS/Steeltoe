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

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    internal class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store
                = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public bool IsAvailable { get; } = true;

        public string Id { get; set; } = "TestSessionId";

        public IEnumerable<string> Keys => _store.Keys;

        public void Clear()
        {
            _store.Clear();
        }

        public Task CommitAsync()
        {
            return Task.FromResult(0);
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return CommitAsync();
        }

        public Task LoadAsync()
        {
            return Task.FromResult(0);
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return LoadAsync();
        }

        public void Remove(string key)
        {
            _store.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }
    }
}
