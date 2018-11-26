using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IViewData
    {
        IView View { get; }
        IDictionary<TagValues, IAggregationData> AggregationMap { get; }
        ITimestamp Start { get; }
        ITimestamp End { get; }
    }
}
