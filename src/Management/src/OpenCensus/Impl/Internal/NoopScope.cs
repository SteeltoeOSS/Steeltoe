using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class NoopScope : IScope
    {
        public static readonly IScope INSTANCE = new NoopScope();

        public static IScope Instance
        {
            get
            {
                return INSTANCE;
            }
        }

        private NoopScope() { }

        public void Dispose()
        {
        }
    }
}
