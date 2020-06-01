// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixRequestContextMiddlewareOwin
    {
        private readonly Func<IDictionary<string, object>, Task> _next;

        public HystrixRequestContextMiddlewareOwin(Func<IDictionary<string, object>, Task> next)
        {
            _next = next;
        }

        public async Task Invoke(IDictionary<string, object> environment)
        {
            var hystrix = HystrixRequestContext.InitializeContext();

            await _next(environment).ConfigureAwait(false);

            hystrix.Dispose();
        }
    }
}
