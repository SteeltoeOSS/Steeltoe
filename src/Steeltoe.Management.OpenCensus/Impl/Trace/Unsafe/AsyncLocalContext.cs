using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Census.Trace.Unsafe
{
    internal static class AsyncLocalContext
    {
        private static AsyncLocal<ISpan> _context = new AsyncLocal<ISpan>();
        public static ISpan CurrentSpan
        {
            get
            {
                return _context.Value;
            }
            set
            {
                _context.Value = value;
            }
        }
    }
}
