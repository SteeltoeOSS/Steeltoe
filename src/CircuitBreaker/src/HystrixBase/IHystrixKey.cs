// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixKey
    {
        string Name { get; }
    }

    public abstract class HystrixKeyDefault : IHystrixKey
    {
        protected HystrixKeyDefault(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}