
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class StatsDefaultTest
    {
        //      [Fact]
        //public void loadStatsManager_UsesProvidedClassLoader()
        //      {
        //          final RuntimeException toThrow = new RuntimeException("UseClassLoader");
        //          thrown.expect(RuntimeException.class);
        //  thrown.expectMessage("UseClassLoader");
        //  Stats.loadStatsComponent(
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
        //  public void loadStatsManager_IgnoresMissingClasses()
        //    {
        //        ClassLoader classLoader =
        //            new ClassLoader() {
        //          @Override
        //              public Class<?> loadClass(String name) throws ClassNotFoundException {
        //            throw new ClassNotFoundException();
        //        }
        //    };

        //    assertThat(Stats.loadStatsComponent(classLoader).getClass().getName())
        //        .isEqualTo("io.opencensus.stats.NoopStats$NoopStatsComponent");
        //}

        [Fact]
        public void DefaultValues()
        {
            Assert.Equal(NoopStats.NoopStatsRecorder, Stats.StatsRecorder);
            Assert.Equal(NoopStats.NewNoopViewManager().GetType(), Stats.ViewManager.GetType());
        }

        [Fact]
        public void GetState()
        {
            Assert.Equal(StatsCollectionState.DISABLED, Stats.State);
        }

    }
}
