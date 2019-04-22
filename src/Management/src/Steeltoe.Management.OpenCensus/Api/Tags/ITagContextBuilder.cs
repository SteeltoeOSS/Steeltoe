using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITagContextBuilder
    {
        ITagContextBuilder Put(ITagKey key, ITagValue value);
        ITagContextBuilder Remove(ITagKey key);
        ITagContext Build();
        IScope BuildScoped();
    }
}
