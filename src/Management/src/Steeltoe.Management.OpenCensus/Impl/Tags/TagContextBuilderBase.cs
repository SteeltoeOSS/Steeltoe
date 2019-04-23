using System;
using System.Collections.Generic;
using System.Text;
using Steeltoe.Management.Census.Common;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public abstract class TagContextBuilderBase : ITagContextBuilder
    {
        public abstract ITagContext Build();
        public abstract IScope BuildScoped();
        public abstract ITagContextBuilder Put(ITagKey key, ITagValue value);
        public abstract ITagContextBuilder Remove(ITagKey key);
    }
}
