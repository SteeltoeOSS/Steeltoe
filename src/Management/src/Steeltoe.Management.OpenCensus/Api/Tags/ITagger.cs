using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    [Obsolete("Use OpenCensus project packages")]
    public interface ITagger
    {
        ITagContext Empty { get; }

        ITagContext CurrentTagContext { get; }

        ITagContextBuilder EmptyBuilder { get; }

        ITagContextBuilder ToBuilder(ITagContext tags);

        ITagContextBuilder CurrentBuilder { get; }
  
        IScope WithTagContext(ITagContext tags);
    }
}
