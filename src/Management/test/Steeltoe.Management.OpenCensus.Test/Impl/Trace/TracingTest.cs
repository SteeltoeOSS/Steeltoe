using Steeltoe.Management.Census.Trace.Config;
using Steeltoe.Management.Census.Trace.Export;
using Steeltoe.Management.Census.Trace.Propagation;

using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class TracingTest
    {
        //@Rule public ExpectedException thrown = ExpectedException.none();

        //      [Fact]
        //public void loadTraceComponent_UsesProvidedClassLoader()
        //      {
        //          final RuntimeException toThrow = new RuntimeException("UseClassLoader");
        //          thrown.expect(RuntimeException.class);
        //  thrown.expectMessage("UseClassLoader");
        //  Tracing.loadTraceComponent(
        //      new ClassLoader()
        //      {
        //          @Override
        //        public Class<?> loadClass(String name)
        //          {
        //              throw toThrow;
        //          }
        //      });
        //}

        //    [Fact]
        //  public void loadTraceComponent_IgnoresMissingClasses()
        //    {
        //        ClassLoader classLoader =
        //            new ClassLoader() {
        //          @Override
        //              public Class<?> loadClass(String name) throws ClassNotFoundException {
        //            throw new ClassNotFoundException();
        //        }
        //    };
        //    assertThat(Tracing.loadTraceComponent(classLoader).getClass().getName())
        //        .isEqualTo("io.opencensus.trace.TraceComponent$NoopTraceComponent");
        //}

        [Fact]
        public void DefaultTracer()
        {
            Assert.Same(Tracer.NoopTracer, Tracing.Tracer);
        }

        [Fact]
        public void DefaultBinaryPropagationHandler()
        {
            Assert.Same(PropagationComponentBase.NoopPropagationComponent, Tracing.PropagationComponent);
        }

        [Fact]
        public void DefaultTraceExporter()
        {
            Assert.Equal(ExportComponentBase.NewNoopExportComponent.GetType(), Tracing.ExportComponent.GetType());
        }

        [Fact]
        public void DefaultTraceConfig()
        {
            Assert.Same(TraceConfigBase.NoopTraceConfig, Tracing.TraceConfig);
        }

        //[Fact]
        //public void ImplementationOfTracer()
        //{
        //    Assert.Type<Tracer>(Tracer()).isInstanceOf(TracerImpl.;
        //}

        //[Fact]
        //public void ImplementationOfBinaryPropagationHandler()
        //{
        //    assertThat(Tracing.getPropagationComponent()).isInstanceOf(PropagationComponent);
        //}

        //[Fact]
        //public void ImplementationOfClock()
        //{
        //    assertThat(Tracing.getClock()).isInstanceOf(MillisClock);
        //}

        //[Fact]
        //public void ImplementationOfTraceExporter()
        //{
        //    assertThat(Tracing.getExportComponent()).isInstanceOf(ExportComponentImpl;
        //}

    }
}
