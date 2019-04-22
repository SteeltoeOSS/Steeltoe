using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public interface ITag
    {
        ITagKey Key { get; }
        ITagValue Value { get; }
    }
}
