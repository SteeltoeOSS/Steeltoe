// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Steeltoe.Common.Retry
{
    public static class RetrySynchronizationManager
    {
        private static AsyncLocal<IRetryContext> _context = new AsyncLocal<IRetryContext>();

        public static IRetryContext GetContext()
        {
            return _context.Value;
        }

        public static IRetryContext Register(IRetryContext context)
        {
            var oldContext = GetContext();
            _context.Value = context;
            return oldContext;
        }

        public static IRetryContext Clear()
        {
            var value = GetContext();
            var parent = value == null ? null : value.Parent;
            _context.Value = parent;
            return value;
        }
    }
}
