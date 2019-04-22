using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public interface IView
    {
        IViewName Name { get; }
        string Description { get; }
        IMeasure Measure { get; }
        IAggregation Aggregation { get; }
        IList<ITagKey> Columns { get; }
    }
}
