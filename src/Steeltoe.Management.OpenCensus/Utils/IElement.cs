using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IElement<T> where T: IElement<T> 
    {
        T Next { get; set; }
        T Previous { get; set; }
    }
}
