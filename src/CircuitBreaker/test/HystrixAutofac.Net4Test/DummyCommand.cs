// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Autofac.Test
{
    internal class DummyCommand : HystrixCommand, IDummyCommand
    {
        private readonly IHystrixCommandOptions _opts;

        public DummyCommand(IHystrixCommandOptions opts)
            : base(opts)
        {
            _opts = opts;
        }

        public HystrixCommandOptions Options
        {
            get
            {
                return _opts as HystrixCommandOptions;
            }
        }
    }

    internal interface IDummyCommand
    {
    }
}
