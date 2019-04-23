using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IAnnotation
    {
        string Description { get; }
        IDictionary<string, IAttributeValue> Attributes { get; }
    }
}
