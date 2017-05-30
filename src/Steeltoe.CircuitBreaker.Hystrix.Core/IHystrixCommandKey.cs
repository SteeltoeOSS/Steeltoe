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

using Steeltoe.CircuitBreaker.Hystrix.Util;

namespace Steeltoe.CircuitBreaker.Hystrix
{

    /// <summary>
    /// A key to represent a <seealso cref="IHystrixCommand"/> for monitoring, circuit-breakers, metrics publishing, caching and other such uses.
    /// </summary>
    public interface IHystrixCommandKey : IHystrixKey
    {

    }

    /// <summary>
    /// Default implementation of the interface
    /// </summary>
    public class HystrixCommandKeyDefault : HystrixKeyDefault<HystrixCommandKeyDefault>, IHystrixCommandKey
    {
  
        internal HystrixCommandKeyDefault(string name) : base(name) { }

        /// <summary>
        /// Retrieve (or create) an interned IHystrixCommandKey instance for a given name.
        /// </summary>
        /// <param name="name"> command name </param>
        /// <returns> IHystrixCommandKey instance that is interned (cached) so a given name will always retrieve the same instance. </returns>
        public static IHystrixCommandKey AsKey(string name)
        {
            return intern.GetOrAddEx(name, k => new HystrixCommandKeyDefault(k));
        }

        public static int CommandCount
        {
            get { return Count; }
        }

    }
}