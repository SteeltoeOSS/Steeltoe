using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Steeltoe.Management.Census.Tags.Unsafe
{
    [Obsolete("Use OpenCensus project packages")]
    internal static class AsyncLocalContext
    {
        private static readonly ITagContext EMPTY_TAG_CONTEXT = new EmptyTagContext();

        private static AsyncLocal<ITagContext> _context = new AsyncLocal<ITagContext>();
        public static ITagContext CurrentTagContext
        {
            get
            {
                if (_context.Value == null)
                {
                    return EMPTY_TAG_CONTEXT;
                }
                return _context.Value;
            }
            set
            {
                if (value == EMPTY_TAG_CONTEXT)
                {
                    _context.Value = null;
                }
                else
                {
                    _context.Value = value;
                }
            }
        }
        internal sealed class EmptyTagContext : TagContextBase
        {

            public override IEnumerator<ITag> GetEnumerator()
            {
                return new List<ITag>().GetEnumerator();
            }
        }
    }
}
