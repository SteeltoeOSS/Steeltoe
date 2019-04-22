using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace.Unsafe;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    internal static class CurrentSpanUtils
    {
        public static ISpan CurrentSpan
        {
            get
            {
                return AsyncLocalContext.CurrentSpan;
            }
        }

        public static IScope WithSpan(ISpan span, bool endSpan)
        {
            return new ScopeInSpan(span, endSpan);
        }

        private sealed class ScopeInSpan : IScope
        {
            private readonly ISpan origContext;
            private readonly ISpan span;
            private bool endSpan;
            public ScopeInSpan(ISpan span, bool endSpan)
            {
                this.span = span;
                this.endSpan = endSpan;
                origContext = AsyncLocalContext.CurrentSpan;
                AsyncLocalContext.CurrentSpan = span;
            }

            public void Dispose()
            {
                var current = AsyncLocalContext.CurrentSpan;
                AsyncLocalContext.CurrentSpan = origContext;

                if (current != origContext)
                {
                    // Log
                }

                if (endSpan)
                {
                    span.End();
                }
            }
        }

    }
}
