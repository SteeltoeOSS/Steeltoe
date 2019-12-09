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

using Steeltoe.CircuitBreaker.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixCommandKey : IHystrixKey
    {
    }

    public class HystrixCommandKeyDefault : HystrixKeyDefault, IHystrixCommandKey
    {
        private static readonly ConcurrentDictionary<string, HystrixCommandKeyDefault> Intern = new ConcurrentDictionary<string, HystrixCommandKeyDefault>();

        internal HystrixCommandKeyDefault(string name)
            : base(name)
        {
        }

        public static IHystrixCommandKey AsKey(string name)
        {
            return Intern.GetOrAddEx(name, k => new HystrixCommandKeyDefault(k));
        }

        public static int CommandCount
        {
            get { return Intern.Count; }
        }
    }
}