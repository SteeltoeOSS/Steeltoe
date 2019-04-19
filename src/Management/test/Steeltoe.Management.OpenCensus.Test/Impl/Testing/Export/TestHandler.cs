using Steeltoe.Management.Census.Trace.Export;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Census.Testing.Export
{
    public class TestHandler : IHandler
    {
        private readonly object monitor = new object();
        private readonly List<ISpanData> spanDataList = new List<ISpanData>();
        public void Export(IList<ISpanData> data)
        {
            lock (monitor)
            {
                this.spanDataList.AddRange(data);
                Monitor.PulseAll(monitor);
            }
            
        }

        public IList<ISpanData> WaitForExport(int numberOfSpans)
        {
            IList<ISpanData> ret;
            lock(monitor) {
                while (spanDataList.Count < numberOfSpans)
                {
                    try
                    {
                        if (!Monitor.Wait(monitor, 5000))
                        {
                            return new List<ISpanData>();
                        }
                    }
                    catch (Exception)
                    {
                        // Preserve the interruption status as per guidance.
                        //Thread.currentThread().interrupt();
                        return new List<ISpanData>();
                    }
                }
                ret = new List<ISpanData>(spanDataList);
                spanDataList.Clear();
            }
            return ret;
        }
    }
}
