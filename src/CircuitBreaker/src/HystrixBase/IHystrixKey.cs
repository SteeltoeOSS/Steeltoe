// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixKey
    {
        string Name { get; }
    }

    public abstract class HystrixKeyDefault : IHystrixKey
    {
        private readonly string name;

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
    }
}