using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Tags
{
    public interface ITagValue
    {
        string AsString { get; }
    }
}
