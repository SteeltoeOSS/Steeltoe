using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITag
    {
        ITagKey Key { get; }
        ITagValue Value { get; }
    }
}
