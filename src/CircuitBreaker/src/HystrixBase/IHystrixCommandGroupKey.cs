// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixCommandGroupKey : IHystrixKey
    {
    }

    // public class HystrixCommandGroupKeyDefault : HystrixKeyDefault<HystrixCommandGroupKeyDefault>, IHystrixCommandGroupKey
    public class HystrixCommandGroupKeyDefault : HystrixKeyDefault, IHystrixCommandGroupKey
    {
        private static readonly ConcurrentDictionary<string, HystrixCommandGroupKeyDefault> Intern = new ConcurrentDictionary<string, HystrixCommandGroupKeyDefault>();

        internal HystrixCommandGroupKeyDefault(string name)
            : base(name)
        {
        }

        public static IHystrixCommandGroupKey AsKey(string name)
        {
            return Intern.GetOrAddEx(name, k => new HystrixCommandGroupKeyDefault(k));
        }

        public static int GroupCount
        {
            get { return Intern.Count; }
        }
    }
}