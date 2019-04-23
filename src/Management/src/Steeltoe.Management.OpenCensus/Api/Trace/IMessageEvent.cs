using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    public interface IMessageEvent
    {
        MessageEventType Type { get; }
        long MessageId { get; }
        long UncompressedMessageSize { get; }
        long CompressedMessageSize { get; }
  
    }
}
