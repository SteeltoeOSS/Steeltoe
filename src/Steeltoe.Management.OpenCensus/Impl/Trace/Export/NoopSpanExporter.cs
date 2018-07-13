using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    internal sealed class NoopSpanExporter : ISpanExporter
    {
        public void AddSpan(ISpan span)
        {
        }

        public void Dispose()
        {
        }

        public void RegisterHandler(string name, IHandler handler)
        {
        }

        public void UnregisterHandler(string name)
        {
        }
    }
}
