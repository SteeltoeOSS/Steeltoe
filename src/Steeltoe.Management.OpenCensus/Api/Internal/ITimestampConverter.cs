using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Internal
{
    public interface ITimestampConverter
    {
        ITimestamp ConvertNanoTime(long nanoTime);
    }
}
