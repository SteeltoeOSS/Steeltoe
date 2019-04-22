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

using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{


    public interface IHystrixKey
    {
        string Name  { get; }
    }

    public abstract class HystrixKeyDefault<T> : IHystrixKey
    {
        internal protected static readonly ConcurrentDictionary<string, T> intern = new ConcurrentDictionary<string, T>();

        internal protected readonly string name;

        public HystrixKeyDefault(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }  
        }

        public override string ToString()
        {
            return name;
        }

        public static int Count
        {
            get { return intern.Count; }
        }

    }

}