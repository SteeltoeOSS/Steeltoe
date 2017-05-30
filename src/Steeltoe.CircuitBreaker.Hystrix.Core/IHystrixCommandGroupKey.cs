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
    /// A group name for a <seealso cref="HystrixCommand"/>. This is used for grouping together commands such as for reporting, alerting, dashboards or team/library ownership.
    /// <para>
    /// By default this will be used to define the <seealso cref="IHystrixThreadPoolKey"/> unless a separate one is defined.
    /// </para>
    /// </summary>
    public interface IHystrixCommandGroupKey : IHystrixKey
    {
    }

    /// <summary>
    /// Default implementation of the interface
    /// </summary>
    public class HystrixCommandGroupKeyDefault : HystrixKeyDefault<HystrixCommandGroupKeyDefault>, IHystrixCommandGroupKey
    {

        internal HystrixCommandGroupKeyDefault(string name) : base(name) { }

        /// <summary>
        /// Retrieve (or create) an interned IHystrixCommandGroupKey instance for a given name.
        /// </summary>
        /// <param name="name"> command name </param>
        /// <returns> IHystrixCommandGroupKey instance that is interned (cached) so a given name will always retrieve the same instance. </returns>
        public static IHystrixCommandGroupKey AsKey(string name)
        {
            return intern.GetOrAddEx(name, k => new HystrixCommandGroupKeyDefault(k));
        }

        public static int GroupCount
        {
            get { return Count; }
        }

    }

}