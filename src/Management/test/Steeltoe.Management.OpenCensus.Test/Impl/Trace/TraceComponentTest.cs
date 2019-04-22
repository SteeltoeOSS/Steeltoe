using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Internal;
using Steeltoe.Management.Census.Trace.Propagation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TraceComponentTest
    {
        private readonly TraceComponent traceComponent = new TraceComponent(DateTimeOffsetClock.GetInstance(), new RandomGenerator(), new SimpleEventQueue());

        [Fact]
        public void ImplementationOfTracer()
        {
            Assert.IsType<Tracer>(traceComponent.Tracer);
        }

        [Fact]
        public void IplementationOfBinaryPropagationHandler()
        {
            Assert.IsType<PropagationComponent>(traceComponent.PropagationComponent);
        }


        [Fact]
        public void ImplementationOfClock()
        {
            Assert.IsType<DateTimeOffsetClock>(traceComponent.Clock);
        }

        [Fact]
        public void ImplementationOfTraceExporter()
        {
            Assert.IsType<ExportComponent>(traceComponent.ExportComponent);
        }
    }
}