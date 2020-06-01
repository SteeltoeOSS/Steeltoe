// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.HystrixBase.Util;
using System;
using System.Web;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixRequestContextModule : IHttpModule
    {
        public void Dispose()
        {
            HystrixShutdown.ShutdownThreads();
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
            context.EndRequest += Context_EndRequest;
        }

        private void Context_EndRequest(object sender, EventArgs e)
        {
            if (sender is HttpApplication app)
            {
                var items = app.Context?.Items;
                if (items != null && items["_hystrix_context_"] is HystrixRequestContext hystrix)
                {
                    hystrix.Dispose();
                }
            }
        }

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            if (sender is HttpApplication app)
            {
                var items = app.Context?.Items;
                if (items != null)
                {
                    items["_hystrix_context_"] = HystrixRequestContext.InitializeContext();
                }
            }
        }
    }
}
