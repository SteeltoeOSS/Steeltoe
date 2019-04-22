using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Utils
{
    public interface IElement<T> where T: IElement<T> 
    {
        T Next { get; set; }
        T Previous { get; set; }
    }
}
